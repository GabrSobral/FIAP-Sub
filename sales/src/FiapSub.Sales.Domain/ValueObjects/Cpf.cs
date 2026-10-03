using FiapSub.Sales.Domain.Exceptions;

namespace FiapSub.Sales.Domain.ValueObjects;

/// <summary>
/// A Brazilian individual taxpayer number. Stored as the 11 bare digits; the mask is a
/// presentation detail. Both check digits are verified, so a typo cannot become a sale.
/// </summary>
public sealed class Cpf : IEquatable<Cpf>
{
    public const int Length = 11;

    private Cpf(string value) => Value = value;

    /// <summary>The 11 digits, unmasked.</summary>
    public string Value { get; }

    /// <summary>Formatted as <c>000.000.000-00</c>.</summary>
    public string Masked =>
        $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}";

    /// <summary>Only the last three digits are kept, for logs and listings.</summary>
    public string Redacted => $"***.***.**{Value[8..9]}-{Value[9..]}";

    public static Cpf Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("CPF is required.");
        }

        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());

        if (digits.Length != Length)
        {
            throw new DomainValidationException($"CPF must have {Length} digits (got '{value}').");
        }

        if (digits.All(digit => digit == digits[0]))
        {
            throw new DomainValidationException("CPF cannot be a sequence of identical digits.");
        }

        if (CheckDigit(digits, 9) != digits[9] || CheckDigit(digits, 10) != digits[10])
        {
            throw new DomainValidationException($"CPF '{value}' has an invalid check digit.");
        }

        return new Cpf(digits);
    }

    /// <summary>
    /// Standard CPF algorithm: weight the preceding digits descending from <c>position + 1</c>,
    /// take the remainder of the sum modulo 11, and anything above 9 becomes 0.
    /// </summary>
    private static char CheckDigit(string digits, int position)
    {
        var sum = 0;

        for (var index = 0; index < position; index++)
        {
            sum += (digits[index] - '0') * (position + 1 - index);
        }

        var remainder = sum * 10 % 11;

        return (char)('0' + (remainder > 9 ? 0 : remainder));
    }

    public bool Equals(Cpf? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as Cpf);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(Cpf? left, Cpf? right) => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Cpf? left, Cpf? right) => !(left == right);

    public override string ToString() => Masked;
}
