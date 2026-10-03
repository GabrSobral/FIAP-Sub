using FiapSub.Sales.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace FiapSub.Sales.Api.Infrastructure;

/// <summary>
/// Translates the exceptions the lower layers are entitled to throw into HTTP status codes.
/// Anything it does not recognise falls through to the default handler and becomes a 500.
/// </summary>
internal sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var (status, title) = exception switch
        {
            DomainValidationException => (StatusCodes.Status400BadRequest, "Invalid data"),
            DomainStateException => (StatusCodes.Status409Conflict, "Conflicting state"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrent modification"),
            DbUpdateException => (StatusCodes.Status409Conflict, "The change conflicts with existing data"),
            _ => (0, string.Empty),
        };

        if (status == 0)
        {
            return false;
        }

        logger.LogInformation(
            exception,
            "Request to {Path} rejected with {Status}: {Message}",
            httpContext.Request.Path,
            status,
            exception.Message);

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message,
            },
        });
    }
}
