using System.Net;
using System.Net.Http.Json;
using FiapSub.Core.Api.Contracts;
using FiapSub.Core.Api.IntegrationTests.Fixtures;
using FiapSub.Core.Application.Contracts;
using FiapSub.Core.Application.Gateways;
using FiapSub.Core.Domain.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace FiapSub.Core.Api.IntegrationTests;

[Collection(CoreApiCollection.Name)]
[Trait("Category", "Integration")]
public class VehicleCatalogueEndpointsTests(CoreApiFixture fixture)
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Register_StoresTheVehicleAndPublishesItToTheSalesService()
    {
        using var response = await fixture.Public.PostAsJsonAsync(
            "api/vehicles",
            ApiHelpers.NewVehicle(55_000m),
            ApiHelpers.Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await response.Content.ReadFromJsonAsync<VehicleResponse>(ApiHelpers.Json, Ct))!;

        created.Id.ShouldNotBe(Guid.Empty);
        created.Status.ShouldBe(nameof(VehicleStatus.Available));
        created.Price.ShouldBe(55_000m);
        created.Currency.ShouldBe("BRL");
        response.Headers.Location!.ToString().ShouldContain(created.Id.ToString());

        // The vehicle really landed in Postgres, not just in memory.
        (await fixture.Public.GetVehicleAsync(created.Id, Ct)).Id.ShouldBe(created.Id);

        await fixture.SalesService.Received().PublishVehicleAsync(
            Arg.Is<VehicleProjectionMessage>(m => m.VehicleId == created.Id && m.Version == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_RejectsInvalidInputWithProblemDetails()
    {
        using var response = await fixture.Public.PostAsJsonAsync(
            "api/vehicles",
            new RegisterVehicleRequest("", "Argo", 1899, "", -1m),
            ApiHelpers.Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiHelpers.Json, Ct))!;
        problem.Extensions["code"]!.ToString().ShouldBe("validation.failed");
        problem.Extensions.ShouldContainKey("errors");
    }

    [Fact]
    public async Task Update_ReplacesTheVehicleData()
    {
        var created = await fixture.Public.RegisterVehicleAsync(cancellationToken: Ct);

        using var response = await fixture.Public.PutAsJsonAsync(
            $"api/vehicles/{created.Id}",
            new UpdateVehicleRequest("Volkswagen", "Polo Track", 2024, "Prata", 92_500.00m),
            ApiHelpers.Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<VehicleResponse>(ApiHelpers.Json, Ct))!;

        updated.Brand.ShouldBe("Volkswagen");
        updated.Price.ShouldBe(92_500.00m);
        updated.Version.ShouldBe(2);

        (await fixture.Public.GetVehicleAsync(created.Id, Ct)).Brand.ShouldBe("Volkswagen");
    }

    [Fact]
    public async Task Update_ReportsNotFound()
    {
        using var response = await fixture.Public.PutAsJsonAsync(
            $"api/vehicles/{Guid.CreateVersion7()}",
            new UpdateVehicleRequest("Fiat", "Argo", 2023, "Azul", 1m),
            ApiHelpers.Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiHelpers.Json, Ct))!;
        problem.Extensions["code"]!.ToString().ShouldBe("vehicle.not_found");
    }

    [Fact]
    public async Task Update_IsRefusedOnceTheVehicleIsReserved()
    {
        var created = await fixture.Public.RegisterVehicleAsync(cancellationToken: Ct);
        await ReserveAsync(created.Id, ApiHelpers.NewPaymentCode());

        using var response = await fixture.Public.PutAsJsonAsync(
            $"api/vehicles/{created.Id}",
            new UpdateVehicleRequest("Fiat", "Argo", 2023, "Azul", 1m),
            ApiHelpers.Json,
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetById_ReportsNotFound()
    {
        using var response = await fixture.Public.GetAsync(
            new Uri($"api/vehicles/{Guid.CreateVersion7()}", UriKind.Relative),
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_FiltersByStatusAndPages()
    {
        await fixture.Public.RegisterVehicleAsync(cancellationToken: Ct);

        using var response = await fixture.Public.GetAsync(
            new Uri("api/vehicles?status=Available&page=1&pageSize=5", UriKind.Relative),
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<PagedResponse>(ApiHelpers.Json, Ct))!;

        page.PageSize.ShouldBe(5);
        page.Items.Count.ShouldBeGreaterThan(0);
        page.Items.ShouldAllBe(v => v.Status == nameof(VehicleStatus.Available));
        page.TotalCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task List_RejectsAnOversizedPage()
    {
        using var response = await fixture.Public.GetAsync(
            new Uri("api/vehicles?pageSize=1000", UriKind.Relative),
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task ReserveAsync(Guid vehicleId, string paymentCode)
    {
        using var response = await fixture.Internal.PostAsJsonAsync(
            $"api/internal/vehicles/{vehicleId}/reserve",
            new PaymentCodeRequest(paymentCode),
            ApiHelpers.Json,
            Ct);

        response.EnsureSuccessStatusCode();
    }

    private sealed record PagedResponse(
        IReadOnlyList<VehicleResponse> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasNextPage);
}
