using System.Net;
using System.Text;
using FiapSub.Sales.Application.Gateways;
using FiapSub.Sales.Infrastructure.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapSub.Sales.Infrastructure.Tests;

/// <summary>
/// The gateway's real job is classifying the catalogue's answers, because the purchase saga
/// compensates differently for "it said no" and "it said nothing".
/// </summary>
public class VehicleCatalogueGatewayTests
{
    private static readonly Guid VehicleId = Guid.CreateVersion7();
    private const string PaymentCode = "PAY-0001";

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    private static (IVehicleCatalogueGateway Gateway, HttpClient Client) Build(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://core-api.test/") };

        return (new VehicleCatalogueGateway(client, NullLogger<VehicleCatalogueGateway>.Instance), client);
    }

    [Fact]
    public async Task Reserve_ReportsSuccessAndCallsTheRightRoute()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var (gateway, client) = Build(handler);
        using var _ = client;

        var reservation = await gateway.ReserveAsync(VehicleId, PaymentCode, Ct);

        reservation.Succeeded.ShouldBeTrue();
        reservation.Outcome.ShouldBe(CatalogueReservationOutcome.Reserved);

        var request = handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Post);
        request.RequestUri!.AbsolutePath.ShouldBe($"/api/internal/vehicles/{VehicleId}/reserve");
        handler.Bodies.ShouldHaveSingleItem().ShouldContain(PaymentCode);
    }

    [Fact]
    public async Task Reserve_MapsA404ToVehicleNotFoundAndKeepsTheProblemDetail()
    {
        var (gateway, client) = Build(new ProblemHandler(
            HttpStatusCode.NotFound, "No vehicle with that id."));
        using var _ = client;

        var reservation = await gateway.ReserveAsync(VehicleId, PaymentCode, Ct);

        reservation.Succeeded.ShouldBeFalse();
        reservation.Outcome.ShouldBe(CatalogueReservationOutcome.VehicleNotFound);
        reservation.Detail.ShouldBe("No vehicle with that id.");
    }

    [Fact]
    public async Task Reserve_MapsA409ToVehicleUnavailable()
    {
        var (gateway, client) = Build(new ProblemHandler(HttpStatusCode.Conflict, "Already reserved."));
        using var _ = client;

        var reservation = await gateway.ReserveAsync(VehicleId, PaymentCode, Ct);

        reservation.Outcome.ShouldBe(CatalogueReservationOutcome.VehicleUnavailable);
        reservation.Detail.ShouldBe("Already reserved.");
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task Reserve_TreatsAnyOtherStatusAsAnAmbiguousFailure(HttpStatusCode statusCode)
    {
        var (gateway, client) = Build(new ProblemHandler(statusCode, "boom"));
        using var _ = client;

        var reservation = await gateway.ReserveAsync(VehicleId, PaymentCode, Ct);

        reservation.Outcome.ShouldBe(CatalogueReservationOutcome.CatalogueUnavailable);
        reservation.Detail.ShouldNotBeNull().ShouldContain(((int)statusCode).ToString());
    }

    [Fact]
    public async Task Reserve_TreatsAnUnreachableCatalogueAsAmbiguous()
    {
        var (gateway, client) = Build(new ThrowingHandler(new HttpRequestException("connection refused")));
        using var _ = client;

        var reservation = await gateway.ReserveAsync(VehicleId, PaymentCode, Ct);

        reservation.Outcome.ShouldBe(CatalogueReservationOutcome.CatalogueUnavailable);
        reservation.Detail.ShouldBe("connection refused");
    }

    [Fact]
    public async Task Reserve_TreatsATimeoutAsAmbiguous()
    {
        var (gateway, client) = Build(new ThrowingHandler(new TaskCanceledException("timed out")));
        using var _ = client;

        var reservation = await gateway.ReserveAsync(VehicleId, PaymentCode, Ct);

        reservation.Outcome.ShouldBe(CatalogueReservationOutcome.CatalogueUnavailable);
    }

    [Fact]
    public async Task Reserve_FallsBackToTheReasonPhraseWhenTheBodyIsNotProblemJson()
    {
        var (gateway, client) = Build(new RecordingHandler(HttpStatusCode.NotFound));
        using var _ = client;

        (await gateway.ReserveAsync(VehicleId, PaymentCode, Ct))
            .Outcome.ShouldBe(CatalogueReservationOutcome.VehicleNotFound);
    }

    [Fact]
    public async Task TryRelease_ReportsWhetherTheHoldWasDropped()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var (gateway, client) = Build(handler);
        using var _ = client;

        (await gateway.TryReleaseAsync(VehicleId, PaymentCode, Ct)).ShouldBeTrue();
        handler.Requests.ShouldHaveSingleItem()
            .RequestUri!.AbsolutePath.ShouldBe($"/api/internal/vehicles/{VehicleId}/release");
    }

    [Fact]
    public async Task TryRelease_NeverThrows_BecauseItRunsOnAFailurePath()
    {
        var (failing, failingClient) = Build(new ThrowingHandler(new HttpRequestException("down")));
        using var _ = failingClient;
        (await failing.TryReleaseAsync(VehicleId, PaymentCode, Ct)).ShouldBeFalse();

        var (rejected, rejectedClient) = Build(new RecordingHandler(HttpStatusCode.Conflict));
        using var __ = rejectedClient;
        (await rejected.TryReleaseAsync(VehicleId, PaymentCode, Ct)).ShouldBeFalse();
    }

    [Fact]
    public async Task Export_MapsThePageIntoCatalogueVehicles()
    {
        var json = $$"""
        {
          "items": [
            {
              "vehicleId": "{{VehicleId}}",
              "brand": "Fiat",
              "model": "Argo Drive",
              "year": 2023,
              "color": "Vermelho",
              "price": 78900.00,
              "currency": "BRL",
              "status": "Available",
              "version": 3,
              "updatedAt": "2026-03-10T12:00:00+00:00"
            }
          ],
          "page": 1,
          "pageSize": 100,
          "totalCount": 1
        }
        """;

        var handler = new JsonHandler(HttpStatusCode.OK, json);
        var (gateway, client) = Build(handler);
        using var _ = client;

        var vehicles = await gateway.ExportAsync(2, 50, Ct);

        var vehicle = vehicles.ShouldHaveSingleItem();
        vehicle.VehicleId.ShouldBe(VehicleId);
        vehicle.Brand.ShouldBe("Fiat");
        vehicle.Price.ShouldBe(78_900.00m);
        vehicle.Version.ShouldBe(3);

        handler.Requests.ShouldHaveSingleItem().RequestUri!.Query.ShouldBe("?page=2&pageSize=50");
    }

    [Fact]
    public async Task Export_ReturnsNothingForANullBody()
    {
        var (gateway, client) = Build(new JsonHandler(HttpStatusCode.OK, "null"));
        using var _ = client;

        (await gateway.ExportAsync(1, 10, Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Export_FailsLoudlyOnANonSuccessResponse()
    {
        var (gateway, client) = Build(new RecordingHandler(HttpStatusCode.InternalServerError));
        using var _ = client;

        await Should.ThrowAsync<HttpRequestException>(() => gateway.ExportAsync(1, 10, Ct));
    }

    [Fact]
    public void OptionsCarryTheInternalTokenHeaderName()
    {
        CatalogueServiceOptions.InternalTokenHeader.ShouldBe("X-Internal-Token");
        new CatalogueServiceOptions().RetryCount.ShouldBe(3);
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }

    private sealed class ProblemHandler(HttpStatusCode statusCode, string detail) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    $"{{\"title\":\"NotFound\",\"detail\":\"{detail}\"}}",
                    Encoding.UTF8,
                    "application/problem+json"),
            });
    }

    private sealed class JsonHandler(HttpStatusCode statusCode, string json) : HttpMessageHandler
    {
        internal List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }
}
