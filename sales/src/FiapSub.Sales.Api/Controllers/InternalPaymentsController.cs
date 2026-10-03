using FiapSub.Sales.Api.Contracts;
using FiapSub.Sales.Api.Infrastructure;
using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Contracts;
using FiapSub.Sales.Application.UseCases.Payments.ApplyPaymentResult;

namespace FiapSub.Sales.Api.Controllers;

/// <summary>
/// Where the catalogue service reports the outcome of a payment. The provider's webhook lands on
/// the catalogue, which owns the vehicle; the sale it belongs to is closed here.
/// </summary>
[Route("api/internal/payments")]
[Produces("application/json")]
public sealed class InternalPaymentsController(
    IUseCase<ApplyPaymentResultCommand, SaleResponse> applyPaymentResult) : ApiControllerBase
{
    /// <summary>Closes or cancels the sale carrying this payment code. Idempotent.</summary>
    [HttpPost("result")]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleResponse>> Result(
        [FromBody] PaymentResultRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await applyPaymentResult.ExecuteAsync(
            new ApplyPaymentResultCommand(request.PaymentCode, request.Status),
            cancellationToken);

        return FromResult(result);
    }
}
