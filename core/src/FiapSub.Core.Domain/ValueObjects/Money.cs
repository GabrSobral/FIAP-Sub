using System.Globalization;
using FiapSub.Core.Domain.Exceptions;

namespace FiapSub.Core.Domain.ValueObjects;

/// <summary>
/// A monetary amount together with its currency. Immutable once created; every factory and
/// operator returns a new instance.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public const string DefaultCurrency = "BRL";
    private const int Scale = 2;

    /// <summary>Required by EF Core when materialising the complex type.</summary>
    private Money()
    {
    }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = DefaultCurrency;

    public static Money Zero { get; } = new(0m, DefaultCurrency);

    public static Money Create(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0m)
        {
            throw new DomainValidationException($"Monetary amount cannot be negative (got {amount.ToString(CultureInfo.InvariantCulture)}).");
        }

        if (decimal.Round(amount, Scale) != amount)
        {
            throw new DomainValidationException($"Monetary amount supports at most {Scale} decimal places (got {amount.ToString(CultureInfo.InvariantCulture)}).");
        }

        return new Money(amount, NormalizeCurrency(currency));
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainValidationException("Currency is required.");
        }

        var normalized = currency.Trim().ToUpperInvariant();

        if (normalized.Length != 3 || !normalized.All(char.IsAsciiLetterUpper))
        {
            throw new DomainValidationException($"Currency must be a 3-letter ISO 4217 code (got '{currency}').");
        }

        return normalized;
    }

    public bool IsGreaterThanZero => Amount > 0m;

    public bool Equals(Money? other) =>
        other is not null && Amount == other.Amount && Currency == other.Currency;

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public static bool operator ==(Money? left, Money? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Money? left, Money? right) => !(left == right);

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Currency} {Amount:0.00}");
}
