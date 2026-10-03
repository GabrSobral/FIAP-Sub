using System.Net.Http.Json;
using System.Text.Json;
using FiapSub.Core.Api.Contracts;
using FiapSub.Core.Application.Contracts;

namespace FiapSub.Core.Api.IntegrationTests.Fixtures;

internal static class ApiHelpers
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    internal static RegisterVehicleRequest NewVehicle(decimal price = 78_900.00m) =>
        new("Fiat", $"Argo {Guid.CreateVersion7():N}"[..20], 2023, "Vermelho", price);

    internal static string NewPaymentCode() => $"PAY-{Guid.CreateVersion7():N}";

    internal static async Task<VehicleResponse> RegisterVehicleAsync(
        this HttpClient client,
        decimal price = 78_900.00m,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync("api/vehicles", NewVehicle(price), Json, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<VehicleResponse>(Json, cancellationToken))!;
    }

    internal static async Task<VehicleResponse> GetVehicleAsync(
        this HttpClient client,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync(new Uri($"api/vehicles/{id}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<VehicleResponse>(Json, cancellationToken))!;
    }
}
