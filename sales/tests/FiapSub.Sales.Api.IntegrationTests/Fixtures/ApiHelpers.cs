using System.Net.Http.Json;
using System.Text.Json;
using FiapSub.Sales.Api.Contracts;
using FiapSub.Sales.Application.Contracts;
using FiapSub.Sales.Application.UseCases.Catalogue.UpsertVehicleSnapshot;

namespace FiapSub.Sales.Api.IntegrationTests.Fixtures;

internal static class ApiHelpers
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    internal const string ValidCpf = "529.982.247-25";
    internal const string OtherValidCpf = "111.444.777-35";

    /// <summary>Pushes a vehicle into the projection the way the catalogue service would.</summary>
    internal static async Task<Guid> PushVehicleAsync(
        this HttpClient internalClient,
        decimal price = 78_900.00m,
        string status = "Available",
        long version = 1,
        Guid? vehicleId = null,
        CancellationToken cancellationToken = default)
    {
        var id = vehicleId ?? Guid.CreateVersion7();

        using var response = await internalClient.PutAsJsonAsync(
            $"api/internal/vehicles/{id}",
            new VehicleProjectionRequest(
                id,
                "Fiat",
                "Argo Drive",
                2023,
                "Vermelho",
                price,
                "BRL",
                status,
                version,
                DateTimeOffset.UtcNow),
            Json,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        (await response.Content.ReadFromJsonAsync<VehicleSnapshotAck>(Json, cancellationToken))!
            .VehicleId.ShouldBe(id);

        return id;
    }

    internal static async Task<SaleResponse> GetSaleAsync(
        this HttpClient client,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(new Uri($"api/sales/{saleId}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<SaleResponse>(Json, cancellationToken))!;
    }

    internal sealed record ListingPage(
        IReadOnlyList<VehicleListingResponse> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasNextPage);
}
