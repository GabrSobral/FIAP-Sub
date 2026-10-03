using FiapSub.Core.Api.Contracts;
using FiapSub.Core.Api.Infrastructure;
using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.UseCases.Payments.ProcessPaymentWebhook;

namespace FiapSub.Core.Api.Controllers;

/// <summary>
/// Entry point for the payment provider. It is deliberately open to the provider rather than
/// behind the internal token, and it is idempotent: replaying a notification changes nothing.
/// </summary>
[Route("api/webhooks/payments")]
[Produces("application/json")]
public sealed class PaymentWebhooksController(
    IUseCase<ProcessPaymentWebhookCommand, PaymentWebhookResponse> processWebhook) : ApiControllerBase
{
    /// <summary>Reports that a payment was approved or cancelled.</summary>
    [HttpPost]
    [ProducesResponseType<PaymentWebhookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentWebhookResponse>> Notify(
        [FromBody] PaymentWebhookRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.TryParseOutcome(out var outcome))
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = nameof(ErrorType.Validation),
                Detail = "Unknown payment status. Use 'approved' or 'cancelled'.",
            };

            problem.Extensions["code"] = ErrorCodes.ValidationFailed;

            return BadRequest(problem);
        }

        var result = await processWebhook.ExecuteAsync(
            new ProcessPaymentWebhookCommand(request.PaymentCode, outcome),
            cancellationToken);

        return FromResult(result);
    }
}
