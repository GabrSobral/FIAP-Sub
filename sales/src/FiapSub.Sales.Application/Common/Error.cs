namespace FiapSub.Sales.Application.Common;

/// <summary>An expected, named failure. Unexpected failures are exceptions, not errors.</summary>
/// <param name="Failures">Per-field messages, when the error came from input validation.</param>
public sealed record Error(
    string Code,
    string Message,
    ErrorType Type,
    IReadOnlyDictionary<string, string[]>? Failures = null)
{
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Validation(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? failures = null) =>
        new(code, message, ErrorType.Validation, failures);
}
