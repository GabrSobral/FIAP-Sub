using FiapSub.Sales.Application.Abstractions.Events;
using FiapSub.Sales.Domain.Abstractions;
using FiapSub.Sales.Domain.Sales.Events;
using Microsoft.Extensions.Logging;

namespace FiapSub.Sales.Application.EventHandlers;

/// <summary>
/// Writes one structured audit line per sale lifecycle transition. A sales service has to be able
/// to answer "what happened to this purchase, and when", and the domain events are exactly that
/// record — committed before this runs, so the log can never claim something that was rolled back.
/// </summary>
public sealed class SaleAuditLogHandler(ILogger<SaleAuditLogHandler> logger) : IDomainEventHandler
{
    public bool CanHandle(IDomainEvent domainEvent) =>
        domainEvent is SaleOpened or SalePaid or SaleCancelled;

    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case SaleOpened opened:
                logger.LogInformation(
                    "Sale {SaleId} opened for vehicle {VehicleId} with payment code {PaymentCode} at {OccurredOn}.",
                    opened.SaleId,
                    opened.VehicleId,
                    opened.PaymentCode,
                    opened.OccurredOn);
                break;

            case SalePaid paid:
                logger.LogInformation(
                    "Sale {SaleId} for vehicle {VehicleId} was paid; the sale date is {SaleDate}.",
                    paid.SaleId,
                    paid.VehicleId,
                    paid.SaleDate);
                break;

            case SaleCancelled cancelled:
                logger.LogInformation(
                    "Sale {SaleId} for vehicle {VehicleId} was cancelled: {Reason}",
                    cancelled.SaleId,
                    cancelled.VehicleId,
                    cancelled.Reason);
                break;
        }

        return Task.CompletedTask;
    }
}
