namespace FiapSub.Core.Domain.Exceptions;

/// <summary>
/// The operation is legal in principle but not from the aggregate's current state
/// (e.g. selling a vehicle that is already sold). Surfaces as HTTP 409.
/// </summary>
public sealed class DomainStateException(string message) : DomainException(message);
