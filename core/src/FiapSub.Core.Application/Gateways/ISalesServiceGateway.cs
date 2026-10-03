namespace FiapSub.Core.Application.Gateways;

/// <summary>
/// Outbound HTTP contract towards the sales service. Both calls are fire-and-reconcile: they are
/// retried by the resilience pipeline and are idempotent on the receiving end.
/// </summary>
public interface ISalesServiceGateway
{
    Task PublishVehicleAsync(VehicleProjectionMessage message, CancellationToken cancellationToken);

    Task PublishPaymentResultAsync(PaymentResultMessage message, CancellationToken cancellationToken);
}
