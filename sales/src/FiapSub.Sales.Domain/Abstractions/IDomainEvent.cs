namespace FiapSub.Sales.Domain.Abstractions;

/// <summary>
/// Something meaningful that happened inside the domain. Events are collected on the aggregate
/// that raised them and dispatched by the unit of work right after the transaction commits.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
