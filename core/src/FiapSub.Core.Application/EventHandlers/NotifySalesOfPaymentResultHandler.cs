using FiapSub.Core.Application.Abstractions.Events;
using FiapSub.Core.Application.Gateways;
using FiapSub.Core.Domain.Abstractions;
using FiapSub.Core.Domain.Vehicles.Events;

namespace FiapSub.Core.Application.EventHandlers;

/// <summary>
/// The sales service owns the sale record, so once a payment settles here it has to be told how
/// it ended in order to close or cancel that sale.
/// </summary>
public sealed class NotifySalesOfPaymentResultHandler(ISalesServiceGateway salesService) : IDomainEventHandler
{
    public bool CanHandle(IDomainEvent domainEvent) =>
        domainEvent is VehicleSold or VehicleReservationReleased;

    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) => domainEvent switch
    {
        VehicleSold sold => salesService.PublishPaymentResultAsync(
            new PaymentResultMessage(sold.PaymentCode, PaymentResultMessage.Approved, sold.VehicleId, sold.OccurredOn),
            cancellationToken),

        VehicleReservationReleased released => salesService.PublishPaymentResultAsync(
            new PaymentResultMessage(released.PaymentCode, PaymentResultMessage.Cancelled, released.VehicleId, released.OccurredOn),
            cancellationToken),

        _ => Task.CompletedTask,
    };
}
