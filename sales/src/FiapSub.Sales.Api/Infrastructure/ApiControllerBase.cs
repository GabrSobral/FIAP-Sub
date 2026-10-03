using FiapSub.Sales.Application.Common;

namespace FiapSub.Sales.Api.Infrastructure;

/// <summary>
/// Turns a use case <see cref="Result"/> into an HTTP response, so each controller action stays a
/// single call plus a mapping.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<TValue> FromResult<TValue>(Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? Ok(result.Value) : ToProblem<TValue>(result.Error);
    }

    protected ActionResult<TValue> FromResult<TValue>(Result<TValue> result, Func<TValue, ActionResult<TValue>> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.IsSuccess ? onSuccess(result.Value) : ToProblem<TValue>(result.Error);
    }

    private ActionResult<TValue> ToProblem<TValue>(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Type.ToString(),
            Detail = error.Message,
        };

        problem.Extensions["code"] = error.Code;

        if (error.Failures is not null)
        {
            problem.Extensions["errors"] = error.Failures;
        }

        return new ObjectResult(problem) { StatusCode = status };
    }
}
