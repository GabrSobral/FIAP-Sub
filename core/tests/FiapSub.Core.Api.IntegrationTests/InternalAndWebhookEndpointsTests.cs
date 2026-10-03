using System.Net;
using System.Net.Http.Json;
using FiapSub.Core.Api.Contracts;
using FiapSub.Core.Api.IntegrationTests.Fixtures;
using FiapSub.Core.Application.Contracts;
using FiapSub.Core.Application.Gateways;
using FiapSub.Core.Application.UseCases.Payments.ProcessPaymentWebhook;
using FiapSub.Core.Domain.Payments;
using FiapSub.Core.Domain.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace FiapSub.Core.Api.IntegrationTests;

[Collection(CoreApiCollection.Name)]
[Trait("Category", "Integration")]
public class InternalAndWebhookEndpointsTests(CoreApiFixture fixture)
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task InternalRoutes_RejectACallWithoutTheSharedToken()
    {
        using var response = await fixture.Public.PostAsJsonAsync(
            $"api/internal/vehicles/{Guid.CreateVersion7()}/reserve",
            new PaymentCodeRequest("PAY-1"),
            ApiHelpers.Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InternalRoutes_RejectAWrongToken()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri("api/internal/vehicles", UriKind.Relative));
        request.Headers.Add("X-Internal-Token", "not-the-token");

        using var response = await fixture.Public.SendAsync(request, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reserve_HoldsTheVehicleAndIsIdempotent()
    {
        var vehicle = await fixture.Public.RegisterVehicleAsync(cancellationToken: Ct);
        var paymentCode = ApiHelpers.NewPaymentCode();

        var reserved = await ReserveAsync(vehicle.Id, paymentCode);
        reserved.Status.ShouldBe(nameof(VehicleStatus.Reserved));
        reserved.PaymentCode.ShouldBe(paymentCode);
        reserved.Version.ShouldBe(2);

        // The sales service retries its reserve call; the second one must change nothing.
        var replayed = await ReserveAsync(vehicle.Id, paymentCode);
        replayed.Version.ShouldBe(2);
    }

    [Fact]
    public async Task Reserve_RefusesAVehicleSomeoneElseIsBuying()
    {
        var vehicle = await fixture.Public.RegisterVehicleAsync(cancellationToken: Ct);
        await ReserveAsync(vehicle.Id, ApiHelpers.NewPaymentCode());

        using var response = await fixture.Internal.PostAsJsonAsync(
            $"api/internal/vehicles/{vehicle.Id}/reserve",
            new PaymentCodeRequest(ApiHelpers.NewPaymentCode()),
            ApiHelpers.Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reserve_ReportsAnUnknownVehicle()
    {
        using var response = await fixture.Internal.PostAsJsonAsync(
            $"api/internal/vehicles/{Guid.CreateVersion7()}/reserve",
            new PaymentCodeRequest(ApiHelpers.NewPaymentCode()),
            ApiHelpers.Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Release_PutsTheVehicleBackOnTheShelf()
    {
        var vehicle = await fixture.Public.RegisterVehicleAsync(cancellationToken: Ct);
        var paymentCode = ApiHelpers.NewPaymentCode();
        await ReserveAsync(vehicle.Id, paymentCode);

        using var response = await fixture.Internal.PostAsJsonAsync(
            $"api/internal/vehicles/{vehicle.Id}/release",
            new PaymentCodeRequest(paymentCode),
            ApiHelpers.Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var released = (await response.Content.ReadFromJsonAsync<VehicleResponse>(ApiHelpers.Json, Ct))!;

        released.Status.ShouldBe(nameof(VehicleStatus.Available));
        released.PaymentCode.ShouldBeNull();
    }

    [Fact]
    public async Task Export_ReturnsTheProjectionPayload()
    {
        var vehicle = await fixture.Public.RegisterVehicleAsync(cancellationToken: Ct);

        using var response = await fixture.Internal.GetAsync(
            new Uri("api/internal/vehicles?page=1&pageSize=500", UriKind.Relative),
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<ProjectionPage>(ApiHelpers.Json, Ct))!;

        page.Items.ShouldContain(item => item.VehicleId == vehicle.Id);
    }

    [Fact]
    public async Task ApprovedWebhook_SellsTheVehicleAndTellsTheSalesService()
    {
        var vehicle = await fixture.Public.RegisterVehicleAsync(cancellationToken: Ct);
        var paymentCode = ApiHelpers.NewPaymentCode();
        await ReserveAsync(vehicle.Id, paymentCode);

        var first = await NotifyPaymentAsync(paymentCode, "approved");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = (await first.Content.ReadFromJsonAsync<PaymentWebhookResponse>(ApiHelpers.Json, Ct))!;
        body.AlreadyProcessed.ShouldBeFalse();
        body.VehicleStatus.ShouldBe(nameof(VehicleStatus.Sold));
        first.Dispose();

        (await fixture.Public.GetVehicleAsync(vehicle.Id, Ct)).Status.ShouldBe(nameof(VehicleStatus.Sold));

        await fixture.SalesService.Received().PublishPaymentResultAsync(
            Arg.Is<PaymentResultMessage>(m =>
                m.PaymentCode == paymentCode
                && m.Status == PaymentResultMessage.Approved
                && m.VehicleId == vehicle.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplayedWebhook_ChangesNothing()
    {
        var vehicle = await fixture.Public.RegisterVehicleAsync(cancellationToken: Ct);
        var paymentCode = ApiHelpers.NewPaymentCode();
        await ReserveAsync(vehicle.Id, paymentCode);

        using (var first = await NotifyPaymentAsync(paymentCode, "approved"))
        {
            first.EnsureSuccessStatusCode();
        }

        var versionAfterFirst = (await fixture.Public.GetVehicleAsync(vehicle.Id, Ct)).Version;

        using var replay = await NotifyPaymentAsync(paymentCode, "approved");
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = (await replay.Content.ReadFromJsonAsync<PaymentWebhookResponse>(ApiHelpers.Json, Ct))!;

        body.AlreadyProcessed.ShouldBeTrue();
        body.VehicleId.ShouldBe(vehicle.Id);
        (await fixture.Public.GetVehicleAsync(vehicle.Id, Ct)).Version.ShouldBe(versionAfterFirst);
    }

    [Fact]
    public async Task CancelledWebhook_ReturnsTheVehicleToTheShelf()
    {
        var vehicle = await fixture.Public.RegisterVehicleAsync(cancellationToken: Ct);
        var paymentCode = ApiHelpers.NewPaymentCode();
        await ReserveAsync(vehicle.Id, paymentCode);

        using var response = await NotifyPaymentAsync(paymentCode, "cancelled");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var reloaded = await fixture.Public.GetVehicleAsync(vehicle.Id, Ct);
        reloaded.Status.ShouldBe(nameof(VehicleStatus.Available));
        reloaded.PaymentCode.ShouldBeNull();

        await fixture.SalesService.Received().PublishPaymentResultAsync(
            Arg.Is<PaymentResultMessage>(m =>
                m.PaymentCode == paymentCode && m.Status == PaymentResultMessage.Cancelled),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Webhook_ReportsAPaymentCodeNoVehicleHolds()
    {
        using var response = await NotifyPaymentAsync(ApiHelpers.NewPaymentCode(), "approved");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiHelpers.Json, Ct))!;
        problem.Extensions["code"]!.ToString().ShouldBe("payment.vehicle_not_found");
    }

    [Fact]
    public async Task Webhook_RejectsAnUnknownStatus()
    {
        using var response = await NotifyPaymentAsync(ApiHelpers.NewPaymentCode(), "half-paid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiHelpers.Json, Ct))!;
        problem.Detail.ShouldNotBeNull().ShouldContain("Unknown payment status");
    }

    [Theory]
    [InlineData("APPROVED", nameof(PaymentOutcome.Approved))]
    [InlineData("paid", nameof(PaymentOutcome.Approved))]
    [InlineData("aprovado", nameof(PaymentOutcome.Approved))]
    [InlineData("canceled", nameof(PaymentOutcome.Cancelled))]
    [InlineData("rejected", nameof(PaymentOutcome.Cancelled))]
    public async Task Webhook_AcceptsTheProviderWordingVariants(string status, string expectedOutcome)
    {
        var vehicle = await fixture.Public.RegisterVehicleAsync(cancellationToken: Ct);
        var paymentCode = ApiHelpers.NewPaymentCode();
        await ReserveAsync(vehicle.Id, paymentCode);

        using var response = await NotifyPaymentAsync(paymentCode, status);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<PaymentWebhookResponse>(ApiHelpers.Json, Ct))!;
        body.Outcome.ShouldBe(expectedOutcome);
    }

    [Fact]
    public async Task HealthEndpoints_ReportTheServiceAndItsDatabase()
    {
        using var live = await fixture.Public.GetAsync(new Uri("health/live", UriKind.Relative), Ct);
        live.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var ready = await fixture.Public.GetAsync(new Uri("health/ready", UriKind.Relative), Ct);
        ready.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SwaggerDocumentIsServed()
    {
        using var response = await fixture.Public.GetAsync(new Uri("swagger/v1/swagger.json", UriKind.Relative), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("/api/webhooks/payments");
    }

    private async Task<VehicleResponse> ReserveAsync(Guid vehicleId, string paymentCode)
    {
        using var response = await fixture.Internal.PostAsJsonAsync(
            $"api/internal/vehicles/{vehicleId}/reserve",
            new PaymentCodeRequest(paymentCode),
            ApiHelpers.Json,
            Ct);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<VehicleResponse>(ApiHelpers.Json, Ct))!;
    }

    private Task<HttpResponseMessage> NotifyPaymentAsync(string paymentCode, string status) =>
        fixture.Public.PostAsJsonAsync(
            "api/webhooks/payments",
            new PaymentWebhookRequest(paymentCode, status),
            ApiHelpers.Json,
            Ct);

    private sealed record ProjectionPage(IReadOnlyList<VehicleProjectionMessage> Items, int TotalCount);
}
