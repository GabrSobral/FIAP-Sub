using FiapSub.Core.Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace FiapSub.Core.Application.Abstractions.Events;

/// <summary>
/// Fans each event out to every handler that claims it. Handlers run after the commit, so a
/// handler that throws must not fail the request: the state change already happened and the
/// cross-service push is reconciled by the backfill endpoint. Failures are logged instead.
/// </summary>
public sealed class DomainEventDispatcher(
    IEnumerable<IDomainEventHandler> handlers,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            foreach (var handler in handlers.Where(h => h.CanHandle(domainEvent)))
            {
                try
                {
                    await handler.HandleAsync(domainEvent, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(
                        exception,
                        "Handler {Handler} failed for domain event {DomainEvent}. The transaction is already committed; the sales service will be reconciled by the backfill endpoint.",
                        handler.GetType().Name,
                        domainEvent.GetType().Name);
                }
            }
        }
    }
}
