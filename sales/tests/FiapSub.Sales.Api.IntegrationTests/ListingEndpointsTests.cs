using System.Net;
using System.Net.Http.Json;
using FiapSub.Sales.Api.Contracts;
using FiapSub.Sales.Api.IntegrationTests.Fixtures;
using FiapSub.Sales.Application.UseCases.Catalogue.UpsertVehicleSnapshot;
using FiapSub.Sales.Domain.Catalog;

namespace FiapSub.Sales.Api.IntegrationTests;

[Collection(SalesApiCollection.Name)]
[Trait("Category", "Integration")]
public class ListingEndpointsTests(SalesApiFixture fixture)
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ForSale_ReturnsAvailableVehiclesOrderedFromCheapestToDearest()
    {
        await fixture.Internal.PushVehicleAsync(price: 310_000m, cancellationToken: Ct);
        await fixture.Internal.PushVehicleAsync(price: 11_500m, cancellationToken: Ct);
        await fixture.Internal.PushVehicleAsync(price: 120_000m, cancellationToken: Ct);

        using var response = await fixture.Public.GetAsync(
            new Uri("api/vehicles/for-sale?page=1&pageSize=100", UriKind.Relative),
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<ApiHelpers.ListingPage>(ApiHelpers.Json, Ct))!;

        page.Items.ShouldAllBe(item => item.Status == nameof(VehicleAvailability.Available));
        page.Items.Select(item => item.Price).ShouldBeInOrder();
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Sold_ReturnsSoldVehiclesOrderedFromCheapestToDearest()
    {
        await fixture.Internal.PushVehicleAsync(price: 95_000m, status: "Sold", cancellationToken: Ct);
        await fixture.Internal.PushVehicleAsync(price: 21_000m, status: "Sold", cancellationToken: Ct);

        using var response = await fixture.Public.GetAsync(
            new Uri("api/vehicles/sold?page=1&pageSize=100", UriKind.Relative),
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<ApiHelpers.ListingPage>(ApiHelpers.Json, Ct))!;

        page.Items.ShouldAllBe(item => item.Status == nameof(VehicleAvailability.Sold));
        page.Items.Select(item => item.Price).ShouldBeInOrder();
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ReservedVehiclesAppearInNeitherListing()
    {
        var vehicleId = await fixture.Internal.PushVehicleAsync(status: "Reserved", cancellationToken: Ct);

        (await ListingIdsAsync("for-sale")).ShouldNotContain(vehicleId);
        (await ListingIdsAsync("sold")).ShouldNotContain(vehicleId);
    }

    [Theory]
    [InlineData("for-sale")]
    [InlineData("sold")]
    public async Task ListingsRejectAnOversizedPage(string route)
    {
        using var response = await fixture.Public.GetAsync(
            new Uri($"api/vehicles/{route}?pageSize=1000", UriKind.Relative),
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProjectionPush_IsIdempotentByVersion()
    {
        var vehicleId = await fixture.Internal.PushVehicleAsync(price: 50_000m, version: 2, cancellationToken: Ct);

        // A newer version wins.
        var newer = await PushAsync(vehicleId, 60_000m, "Reserved", version: 3);
        newer.Applied.ShouldBeTrue();
        newer.Status.ShouldBe(nameof(VehicleAvailability.Reserved));

        // A replay of an older version changes nothing.
        var stale = await PushAsync(vehicleId, 1m, "Sold", version: 2);
        stale.Applied.ShouldBeFalse();
        stale.Version.ShouldBe(3);
        stale.Status.ShouldBe(nameof(VehicleAvailability.Reserved));
    }

    [Fact]
    public async Task ProjectionPush_RejectsAnUnknownStatus()
    {
        var vehicleId = Guid.CreateVersion7();

        using var response = await fixture.Internal.PutAsJsonAsync(
            $"api/internal/vehicles/{vehicleId}",
            new VehicleProjectionRequest(
                vehicleId, "Fiat", "Argo", 2023, "Vermelho", 1m, "BRL", "Scrapped", 1, DateTimeOffset.UtcNow),
            ApiHelpers.Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProjectionPush_RequiresTheInternalToken()
    {
        var vehicleId = Guid.CreateVersion7();

        using var response = await fixture.Public.PutAsJsonAsync(
            $"api/internal/vehicles/{vehicleId}",
            new VehicleProjectionRequest(
                vehicleId, "Fiat", "Argo", 2023, "Vermelho", 1m, "BRL", "Available", 1, DateTimeOffset.UtcNow),
            ApiHelpers.Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthEndpointsReportTheServiceAndItsDatabase()
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
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("/api/vehicles/for-sale");
    }

    private async Task<VehicleSnapshotAck> PushAsync(Guid vehicleId, decimal price, string status, long version)
    {
        using var response = await fixture.Internal.PutAsJsonAsync(
            $"api/internal/vehicles/{vehicleId}",
            new VehicleProjectionRequest(
                vehicleId, "Fiat", "Argo Drive", 2023, "Vermelho", price, "BRL", status, version, DateTimeOffset.UtcNow),
            ApiHelpers.Json,
            Ct);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<VehicleSnapshotAck>(ApiHelpers.Json, Ct))!;
    }

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
