namespace FiapSub.Core.Domain.Exceptions;

/// <summary>The data handed to the domain is not acceptable. Surfaces as HTTP 400.</summary>
public sealed class DomainValidationException(string message) : DomainException(message);
