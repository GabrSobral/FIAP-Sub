using FiapSub.Sales.Api.Contracts;
using FiapSub.Sales.Api.Infrastructure;
using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Contracts;
using FiapSub.Sales.Application.UseCases.Sales.GetSale;
using FiapSub.Sales.Application.UseCases.Sales.OpenSale;

namespace FiapSub.Sales.Api.Controllers;

/// <summary>Purchases. This service owns the sale record; the catalogue owns the vehicle.</summary>
[Route("api/sales")]
[Produces("application/json")]
public sealed class SalesController(
    IUseCase<OpenSaleCommand, SaleResponse> openSale,
    IUseCase<GetSaleQuery, SaleResponse> getSale) : ApiControllerBase
{
    /// <summary>
    /// Buys a vehicle. Returns the payment code the buyer must quote to the payment provider;
    /// the sale only closes once that payment is approved.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleResponse>> Open(
        [FromBody] OpenSaleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await openSale.ExecuteAsync(
            new OpenSaleCommand(request.VehicleId, request.Cpf, request.SaleDate),
            cancellationToken);

        return FromResult(result, sale => CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale));
    }

    /// <summary>Fetches a purchase, including its payment code and sale date.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        FromResult(await getSale.ExecuteAsync(new GetSaleQuery(id), cancellationToken));
}
