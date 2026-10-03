namespace FiapSub.Core.Domain.Abstractions;

/// <summary>
/// Implemented by aggregate roots so the persistence layer can drain their events after commit
/// without knowing the concrete aggregate types.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
