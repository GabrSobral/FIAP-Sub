using FiapSub.Core.Application.Common;

namespace FiapSub.Core.Application.Abstractions;

/// <summary>A single application operation that returns a value.</summary>
public interface IUseCase<in TRequest, TResponse>
{
    Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>A single application operation with no return value.</summary>
public interface IUseCase<in TRequest>
{
    Task<Result> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
}
