using System.Net.Http.Json;
using FiapSub.Core.Application.Gateways;
using Microsoft.Extensions.Logging;

namespace FiapSub.Core.Infrastructure.Http;

/// <summary>
/// Outbound half of the service-to-service contract. Both calls target idempotent endpoints, so
/// the resilience pipeline is free to retry them.
/// </summary>
internal sealed class SalesServiceGateway(HttpClient httpClient, ILogger<SalesServiceGateway> logger)
    : ISalesServiceGateway
{
    public async Task PublishVehicleAsync(VehicleProjectionMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var response = await httpClient.PutAsJsonAsync(
            $"api/internal/vehicles/{message.VehicleId}",
            message,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        logger.LogInformation(
            "Published vehicle {VehicleId} version {Version} to the sales service.",
            message.VehicleId,
            message.Version);
    }

    public async Task PublishPaymentResultAsync(PaymentResultMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var response = await httpClient.PostAsJsonAsync(
            "api/internal/payments/result",
            message,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        logger.LogInformation(
            "Reported payment {PaymentCode} as {Status} to the sales service.",
            message.PaymentCode,
            message.Status);
    }
}
