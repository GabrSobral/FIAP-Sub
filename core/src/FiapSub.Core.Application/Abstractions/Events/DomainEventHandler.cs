using FiapSub.Core.Domain.Abstractions;

namespace FiapSub.Core.Application.Abstractions.Events;

/// <summary>Convenience base for handlers interested in exactly one event type.</summary>
public abstract class DomainEventHandler<TEvent> : IDomainEventHandler
    where TEvent : IDomainEvent
{
    public bool CanHandle(IDomainEvent domainEvent) => domainEvent is TEvent;

    public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) =>
        HandleAsync((TEvent)domainEvent, cancellationToken);

    protected abstract Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
