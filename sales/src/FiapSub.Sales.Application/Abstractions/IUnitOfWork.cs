namespace FiapSub.Sales.Application.Abstractions;

/// <summary>
/// Commits everything the current request changed as one transaction, then dispatches the domain
/// events the saved aggregates raised.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
