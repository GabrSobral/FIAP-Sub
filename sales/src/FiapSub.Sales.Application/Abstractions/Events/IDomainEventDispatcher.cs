using FiapSub.Sales.Domain.Abstractions;

namespace FiapSub.Sales.Application.Abstractions.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}
