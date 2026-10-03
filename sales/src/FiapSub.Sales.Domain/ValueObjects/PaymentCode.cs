using FiapSub.Sales.Domain.Exceptions;

namespace FiapSub.Sales.Domain.ValueObjects;

/// <summary>
/// The identifier the payment provider uses for a transaction. It is the only thing the
/// incoming webhook gives us, so it is also the key that ties a payment back to a vehicle.
/// </summary>
public sealed class PaymentCode : IEquatable<PaymentCode>
{
    public const int MaxLength = 64;

    private PaymentCode(string value) => Value = value;

    public string Value { get; }

    public static PaymentCode Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Payment code is required.");
        }

        var normalized = value.Trim();

        if (normalized.Length > MaxLength)
        {
            throw new DomainValidationException($"Payment code must be at most {MaxLength} characters long.");
        }

        if (!normalized.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            throw new DomainValidationException($"Payment code accepts only letters, digits, '-' and '_' (got '{value}').");
        }

        return new PaymentCode(normalized);
    }

    public bool Equals(PaymentCode? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as PaymentCode);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(PaymentCode? left, PaymentCode? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(PaymentCode? left, PaymentCode? right) => !(left == right);

    public override string ToString() => Value;
}
