namespace FiapSub.Sales.Domain.Exceptions;

/// <summary>Base type for every rule the domain itself refuses to break.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
