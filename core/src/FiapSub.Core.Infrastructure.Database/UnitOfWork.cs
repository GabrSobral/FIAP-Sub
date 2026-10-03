using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Abstractions.Events;
using FiapSub.Core.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FiapSub.Core.Infrastructure.Database;

/// <summary>
/// Commits the request's changes, then publishes the events the saved aggregates raised.
/// Events are drained before the save so a handler can never see them twice, and dispatched
/// after it so no handler reacts to a change that was rolled back.
/// </summary>
internal sealed class UnitOfWork(CoreDbContext context, IDomainEventDispatcher dispatcher) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var tracked = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        var domainEvents = tracked.SelectMany(aggregate => aggregate.DomainEvents).ToList();

        foreach (var aggregate in tracked)
        {
            aggregate.ClearDomainEvents();
        }

        var affected = await context.SaveChangesAsync(cancellationToken);

        if (domainEvents.Count > 0)
        {
            await dispatcher.DispatchAsync(domainEvents, cancellationToken);
        }

        return affected;
    }
}
