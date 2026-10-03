using System.Net;
using System.Net.Http.Json;
using FiapSub.Sales.Api.Contracts;
using FiapSub.Sales.Api.IntegrationTests.Fixtures;
using FiapSub.Sales.Application.Contracts;
using FiapSub.Sales.Application.Gateways;
using FiapSub.Sales.Domain.Catalog;
using FiapSub.Sales.Domain.Sales;
using Microsoft.AspNetCore.Mvc;

namespace FiapSub.Sales.Api.IntegrationTests;

[Collection(SalesApiCollection.Name)]
[Trait("Category", "Integration")]
public class PurchaseEndpointsTests(SalesApiFixture fixture)
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    private void GivenTheCatalogueAccepts() =>
        fixture.Catalogue
            .ReserveAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CatalogueReservation.Reserved());

    private void GivenTheCatalogueAnswers(CatalogueReservation reservation) =>
        fixture.Catalogue
            .ReserveAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(reservation);

    [Fact]
    public async Task Purchase_OpensAPendingSaleAndHidesTheVehicleFromTheForSaleListing()
    {
        GivenTheCatalogueAccepts();
        var vehicleId = await fixture.Internal.PushVehicleAsync(price: 64_900m, cancellationToken: Ct);

        using var response = await OpenAsync(vehicleId, ApiHelpers.ValidCpf);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var sale = (await response.Content.ReadFromJsonAsync<SaleResponse>(ApiHelpers.Json, Ct))!;

        sale.VehicleId.ShouldBe(vehicleId);
        sale.Status.ShouldBe(nameof(SaleStatus.PendingPayment));
        sale.Amount.ShouldBe(64_900m);
        sale.BuyerCpf.ShouldBe("529.982.247-25");
        sale.PaymentCode.ShouldStartWith("PAY-");
        sale.SaleDate.ShouldBeNull();
        response.Headers.Location!.ToString().ShouldContain(sale.Id.ToString());

        // The catalogue was asked to hold it, with the code we generated.
        await fixture.Catalogue.Received()
            .ReserveAsync(vehicleId, sale.PaymentCode, Arg.Any<CancellationToken>());

        (await fixture.Public.GetSaleAsync(sale.Id, Ct)).Id.ShouldBe(sale.Id);
        (await ForSaleIdsAsync()).ShouldNotContain(vehicleId);
    }

    [Fact]
    public async Task Purchase_RefusesAVehicleTheProjectionDoesNotKnow()
    {
        GivenTheCatalogueAccepts();

        using var response = await OpenAsync(Guid.CreateVersion7(), ApiHelpers.ValidCpf);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiHelpers.Json, Ct))!;
        problem.Extensions["code"]!.ToString().ShouldBe("vehicle.not_in_catalogue");
    }

    [Fact]
    public async Task Purchase_RefusesAnInvalidCpf()
    {
        GivenTheCatalogueAccepts();
        var vehicleId = await fixture.Internal.PushVehicleAsync(cancellationToken: Ct);

        // Eleven digits, so it clears the request validator, but the check digit is wrong.
        using var response = await OpenAsync(vehicleId, "529.982.247-26");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("check digit");
    }

    [Fact]
    public async Task Purchase_RefusesAMalformedCpfBeforeReachingTheDomain()
    {
        GivenTheCatalogueAccepts();
        var vehicleId = await fixture.Internal.PushVehicleAsync(cancellationToken: Ct);

        using var response = await OpenAsync(vehicleId, "123");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiHelpers.Json, Ct))!;
        problem.Extensions["code"]!.ToString().ShouldBe("validation.failed");
    }

    [Fact]
    public async Task Purchase_RefusesASecondBuyerWhileAPaymentIsPending()
    {
        GivenTheCatalogueAccepts();
        var vehicleId = await fixture.Internal.PushVehicleAsync(cancellationToken: Ct);

        using (var first = await OpenAsync(vehicleId, ApiHelpers.ValidCpf))
        {
            first.StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        // The projection is already Reserved locally, so the second buyer is turned away there.
        using var second = await OpenAsync(vehicleId, ApiHelpers.OtherValidCpf);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = (await second.Content.ReadFromJsonAsync<ProblemDetails>(ApiHelpers.Json, Ct))!;
        problem.Extensions["code"]!.ToString().ShouldBe("vehicle.not_available");
    }

    [Fact]
    public async Task Purchase_CancelsTheSaleWhenTheCatalogueSaysTheVehicleIsTaken()
    {
        GivenTheCatalogueAnswers(CatalogueReservation.VehicleUnavailable("already reserved"));
        var vehicleId = await fixture.Internal.PushVehicleAsync(cancellationToken: Ct);

        using var response = await OpenAsync(vehicleId, ApiHelpers.ValidCpf);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiHelpers.Json, Ct))!;
        problem.Extensions["code"]!.ToString().ShouldBe("vehicle.not_available");

        // The compensation ran: the vehicle is free for another attempt once the catalogue frees it.
        (await ForSaleIdsAsync()).ShouldNotContain(vehicleId);
    }

    [Fact]
    public async Task Purchase_PutsTheVehicleBackWhenTheCatalogueCannotBeReached()
    {
        GivenTheCatalogueAnswers(CatalogueReservation.CatalogueUnavailable("timeout"));
        fixture.Catalogue
            .TryReleaseAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var vehicleId = await fixture.Internal.PushVehicleAsync(cancellationToken: Ct);

        using var response = await OpenAsync(vehicleId, ApiHelpers.ValidCpf);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiHelpers.Json, Ct))!;
        problem.Extensions["code"]!.ToString().ShouldBe("catalogue.unavailable");

        // A hold that may have landed is dropped, and the vehicle is listed again.
        await fixture.Catalogue.Received()
            .TryReleaseAsync(vehicleId, Arg.Any<string>(), Arg.Any<CancellationToken>());
        (await ForSaleIdsAsync()).ShouldContain(vehicleId);
    }

    [Fact]
    public async Task GetSale_ReportsAnUnknownSale()
    {
        using var response = await fixture.Public.GetAsync(
            new Uri($"api/sales/{Guid.CreateVersion7()}", UriKind.Relative),
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiHelpers.Json, Ct))!;
        problem.Extensions["code"]!.ToString().ShouldBe("sale.not_found");
    }

    [Fact]
    public async Task ApprovedPayment_ClosesTheSaleAndMovesTheVehicleToTheSoldListing()
    {
        GivenTheCatalogueAccepts();
        var vehicleId = await fixture.Internal.PushVehicleAsync(price: 48_000m, cancellationToken: Ct);
        var sale = await OpenAndReadAsync(vehicleId);

        using var response = await ReportPaymentAsync(sale.PaymentCode, "approved", vehicleId);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var closed = (await response.Content.ReadFromJsonAsync<SaleResponse>(ApiHelpers.Json, Ct))!;

        closed.Status.ShouldBe(nameof(SaleStatus.Paid));
        closed.SaleDate.ShouldNotBeNull();

        (await fixture.Public.GetSaleAsync(sale.Id, Ct)).Status.ShouldBe(nameof(SaleStatus.Paid));
        (await SoldIdsAsync()).ShouldContain(vehicleId);
        (await ForSaleIdsAsync()).ShouldNotContain(vehicleId);
    }

    [Fact]
    public async Task ApprovedPayment_IsIdempotent()
    {
        GivenTheCatalogueAccepts();
        var vehicleId = await fixture.Internal.PushVehicleAsync(cancellationToken: Ct);
        var sale = await OpenAndReadAsync(vehicleId);

        using (var first = await ReportPaymentAsync(sale.PaymentCode, "approved", vehicleId))
        {
            first.EnsureSuccessStatusCode();
        }

        var versionAfterFirst = (await fixture.Public.GetSaleAsync(sale.Id, Ct)).Version;

        using var replay = await ReportPaymentAsync(sale.PaymentCode, "approved", vehicleId);
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await fixture.Public.GetSaleAsync(sale.Id, Ct)).Version.ShouldBe(versionAfterFirst);
    }

    [Fact]
    public async Task CancelledPayment_CancelsTheSaleAndRelistsTheVehicle()
    {
        GivenTheCatalogueAccepts();
        var vehicleId = await fixture.Internal.PushVehicleAsync(price: 33_000m, cancellationToken: Ct);
        var sale = await OpenAndReadAsync(vehicleId);

        using var response = await ReportPaymentAsync(sale.PaymentCode, "cancelled", vehicleId);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cancelled = (await response.Content.ReadFromJsonAsync<SaleResponse>(ApiHelpers.Json, Ct))!;

        cancelled.Status.ShouldBe(nameof(SaleStatus.Cancelled));
        cancelled.SaleDate.ShouldBeNull();
        cancelled.CancellationReason.ShouldNotBeNull();

        (await ForSaleIdsAsync()).ShouldContain(vehicleId);
        (await SoldIdsAsync()).ShouldNotContain(vehicleId);
    }

    [Fact]
    public async Task AfterACancelledPayment_TheVehicleCanBeBoughtAgain()
    {
        GivenTheCatalogueAccepts();
        var vehicleId = await fixture.Internal.PushVehicleAsync(cancellationToken: Ct);
        var first = await OpenAndReadAsync(vehicleId);

        using (var cancel = await ReportPaymentAsync(first.PaymentCode, "cancelled", vehicleId))
        {
            cancel.EnsureSuccessStatusCode();
        }

        using var second = await OpenAsync(vehicleId, ApiHelpers.OtherValidCpf);

        second.StatusCode.ShouldBe(HttpStatusCode.Created);
        var retried = (await second.Content.ReadFromJsonAsync<SaleResponse>(ApiHelpers.Json, Ct))!;
        retried.PaymentCode.ShouldNotBe(first.PaymentCode);
    }

    [Fact]
    public async Task PaymentResult_ReportsAnUnknownPaymentCode()
    {
        using var response = await ReportPaymentAsync("PAY-does-not-exist", "approved", Guid.CreateVersion7());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiHelpers.Json, Ct))!;
        problem.Extensions["code"]!.ToString().ShouldBe("sale.not_found");
    }

    [Fact]
    public async Task PaymentResult_RejectsAnUnknownStatus()
    {
        using var response = await ReportPaymentAsync("PAY-1", "half-paid", Guid.CreateVersion7());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PaymentResult_RequiresTheInternalToken()
    {
        using var response = await fixture.Public.PostAsJsonAsync(
            "api/internal/payments/result",
            new PaymentResultRequest("PAY-1", "approved", Guid.CreateVersion7(), DateTimeOffset.UtcNow),
            ApiHelpers.Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CatalogueSync_PullsTheCatalogueIntoTheProjection()
    {
        var pulled = Guid.CreateVersion7();
        fixture.Catalogue.ExportAsync(1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                new CatalogueVehicle(
                    pulled, "Honda", "Civic", 2022, "Preto", 142_000m, "BRL",
                    nameof(VehicleAvailability.Available), 1, DateTimeOffset.UtcNow),
            ]);

        using var response = await fixture.Internal.PostAsync(
            new Uri("api/internal/catalogue/sync?pageSize=100", UriKind.Relative),
            content: null,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ForSaleIdsAsync()).ShouldContain(pulled);
    }

    private Task<HttpResponseMessage> OpenAsync(Guid vehicleId, string? cpf) =>
        fixture.Public.PostAsJsonAsync(
            "api/sales",
            new OpenSaleRequest(vehicleId, cpf, null),
            ApiHelpers.Json,
            Ct);

    private async Task<SaleResponse> OpenAndReadAsync(Guid vehicleId)
    {
        using var response = await OpenAsync(vehicleId, ApiHelpers.ValidCpf);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<SaleResponse>(ApiHelpers.Json, Ct))!;
    }

    private Task<HttpResponseMessage> ReportPaymentAsync(string paymentCode, string status, Guid vehicleId) =>
        fixture.Internal.PostAsJsonAsync(
            "api/internal/payments/result",
            new PaymentResultRequest(paymentCode, status, vehicleId, DateTimeOffset.UtcNow),
            ApiHelpers.Json,
            Ct);

    private Task<IReadOnlyList<Guid>> ForSaleIdsAsync() => ListingIdsAsync("for-sale");

    private Task<IReadOnlyList<Guid>> SoldIdsAsync() => ListingIdsAsync("sold");

    private async Task<IReadOnlyList<Guid>> ListingIdsAsync(string route)
    {
        using var response = await fixture.Public.GetAsync(
            new Uri($"api/vehicles/{route}?page=1&pageSize=100", UriKind.Relative),
            Ct);

        response.EnsureSuccessStatusCode();
        var page = (await response.Content.ReadFromJsonAsync<ApiHelpers.ListingPage>(ApiHelpers.Json, Ct))!;

        return page.Items.Select(item => item.VehicleId).ToList();
    }
}
