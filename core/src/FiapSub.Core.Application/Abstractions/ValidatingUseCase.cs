using FiapSub.Core.Application.Common;
using FluentValidation;

namespace FiapSub.Core.Application.Abstractions;

/// <summary>
/// Runs the request's FluentValidation validators before the real use case, turning input
/// problems into a <see cref="ErrorType.Validation"/> failure instead of an exception. Wrapping
/// the handler keeps validation out of both the controllers and the handlers themselves.
/// </summary>
public sealed class ValidatingUseCase<TRequest, TResponse>(
    IUseCase<TRequest, TResponse> inner,
    IEnumerable<IValidator<TRequest>> validators) : IUseCase<TRequest, TResponse>
{
    public async Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request!, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count == 0)
        {
            return await inner.ExecuteAsync(request, cancellationToken);
        }

        var byProperty = failures
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        return Result.Failure<TResponse>(Error.Validation(
            ErrorCodes.ValidationFailed,
            "One or more fields are invalid.",
            byProperty));
    }
}
