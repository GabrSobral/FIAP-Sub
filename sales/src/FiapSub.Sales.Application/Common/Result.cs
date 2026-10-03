using System.Diagnostics.CodeAnalysis;

namespace FiapSub.Sales.Application.Common;

/// <summary>
/// The outcome of a use case: either it worked, or it failed for a reason the caller is meant to
/// handle. Keeps expected failures out of the exception path.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.FromValue(value);

    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.FromError(error);
}

/// <inheritdoc cref="Result"/>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(bool isSuccess, TValue? value, Error? error) : base(isSuccess, error) => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result carries no value; inspect Error instead.");

    internal static Result<TValue> FromValue(TValue value) => new(true, value, null);

    internal static Result<TValue> FromError(Error error) => new(false, default, error);
}
