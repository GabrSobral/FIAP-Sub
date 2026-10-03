using FiapSub.Sales.Domain.Abstractions;

namespace FiapSub.Sales.Application.Abstractions.Events;

/// <summary>
/// Reacts to a domain event after the transaction that produced it has committed.
/// Non-generic on purpose: the dispatcher can then fan out without reflection.
/// </summary>
public interface IDomainEventHandler
{
    bool CanHandle(IDomainEvent domainEvent);

    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
