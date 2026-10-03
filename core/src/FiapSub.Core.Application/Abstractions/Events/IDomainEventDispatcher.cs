using FiapSub.Core.Domain.Abstractions;

namespace FiapSub.Core.Application.Abstractions.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}
