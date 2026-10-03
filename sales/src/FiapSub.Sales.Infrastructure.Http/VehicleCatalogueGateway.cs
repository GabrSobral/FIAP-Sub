using System.Net;
using System.Net.Http.Json;
using FiapSub.Sales.Application.Gateways;
using FiapSub.Sales.Infrastructure.Http.Contracts;
using Microsoft.Extensions.Logging;

namespace FiapSub.Sales.Infrastructure.Http;

/// <summary>
/// Outbound half of the service-to-service contract. Its job is to turn the catalogue's HTTP
/// answers into the small set of outcomes the purchase saga knows how to compensate for —
/// in particular, telling "the catalogue said no" apart from "the catalogue said nothing".
/// </summary>
internal sealed class VehicleCatalogueGateway(
    HttpClient httpClient,
    ILogger<VehicleCatalogueGateway> logger) : IVehicleCatalogueGateway
{
    public async Task<CatalogueReservation> ReserveAsync(
        Guid vehicleId,
        string paymentCode,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"api/internal/vehicles/{vehicleId}/reserve",
                new PaymentCodePayload(paymentCode),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "The catalogue is holding vehicle {VehicleId} for payment {PaymentCode}.",
                    vehicleId,
                    paymentCode);

                return CatalogueReservation.Reserved();
            }

            var detail = await ReadDetailAsync(response, cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => CatalogueReservation.VehicleNotFound(detail),
                HttpStatusCode.Conflict => CatalogueReservation.VehicleUnavailable(detail),
                _ => CatalogueReservation.CatalogueUnavailable($"HTTP {(int)response.StatusCode}: {detail}"),
            };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
                                          && !cancellationToken.IsCancellationRequested)
        {
            // No answer at all: the hold may or may not have been placed.
            logger.LogError(
                exception,
                "Could not reach the catalogue to reserve vehicle {VehicleId}.",
                vehicleId);

            return CatalogueReservation.CatalogueUnavailable(exception.Message);
        }
    }

    public async Task<bool> TryReleaseAsync(
        Guid vehicleId,
        string paymentCode,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"api/internal/vehicles/{vehicleId}/release",
                new PaymentCodePayload(paymentCode),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning(
                "Compensating release of vehicle {VehicleId} answered HTTP {Status}.",
                vehicleId,
                (int)response.StatusCode);

            return false;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // Already on a failure path: log it and let the catalogue's own state be the truth.
            logger.LogError(exception, "Compensating release of vehicle {VehicleId} failed.", vehicleId);

            return false;
        }
    }

    public async Task<IReadOnlyList<CatalogueVehicle>> ExportAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var uri = new Uri($"api/internal/vehicles?page={page}&pageSize={pageSize}", UriKind.Relative);

        using var response = await httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content
            .ReadFromJsonAsync<CataloguePage<CatalogueVehiclePayload>>(cancellationToken);

        if (body is null)
        {
            return [];
        }

        return body.Items
            .Select(item => new CatalogueVehicle(
                item.VehicleId,
                item.Brand,
                item.Model,
                item.Year,
                item.Color,
                item.Price,
                item.Currency,
                item.Status,
                item.Version,
                item.UpdatedAt))
            .ToList();
    }

    private static async Task<string> ReadDetailAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(cancellationToken);

            return problem?.Detail ?? response.ReasonPhrase ?? string.Empty;
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException)
        {
            return response.ReasonPhrase ?? string.Empty;
        }
    }

    private sealed record ProblemPayload(string? Title, string? Detail);
}
