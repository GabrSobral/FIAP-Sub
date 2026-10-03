using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace FiapSub.Core.Api.Infrastructure;

/// <summary>
/// Guards the service-to-service routes with a shared secret. It fails closed: if no token is
/// configured, <c>/api/internal/*</c> answers 503 rather than running unprotected.
/// </summary>
internal sealed class InternalApiTokenMiddleware(
    RequestDelegate next,
    IOptions<InternalApiOptions> options,
    ILogger<InternalApiTokenMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var settings = options.Value;

        if (!context.Request.Path.StartsWithSegments(settings.PathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (string.IsNullOrEmpty(settings.Token))
        {
            logger.LogError(
                "{Section}:{Key} is not configured, so the internal API is disabled.",
                InternalApiOptions.SectionName,
                nameof(InternalApiOptions.Token));

            await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, "The internal API is not configured.");
            return;
        }

        var provided = context.Request.Headers[InternalApiOptions.HeaderName].ToString();

        if (!IsMatch(provided, settings.Token))
        {
            logger.LogWarning("Rejected an internal call to {Path} with a missing or wrong token.", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "A valid internal token is required.");
            return;
        }

        await next(context);
    }

    private static bool IsMatch(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = "Internal API",
            Detail = detail,
        });
    }
}
