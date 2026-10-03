using FiapSub.Sales.Domain.Exceptions;

namespace FiapSub.Sales.Domain.Abstractions;

/// <summary>Small input checks shared by the aggregates in this context.</summary>
internal static class Guard
{
    internal static string RequiredText(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{field} is required.");
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new DomainValidationException($"{field} must be at most {maxLength} characters long.");
        }

        return normalized;
    }
}
