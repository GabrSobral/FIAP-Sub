using FiapSub.Sales.Api.Infrastructure;
using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Contracts;
using FiapSub.Sales.Application.UseCases.Listings.ListSoldVehicles;
using FiapSub.Sales.Application.UseCases.Listings.ListVehiclesForSale;

namespace FiapSub.Sales.Api.Controllers;

/// <summary>
/// The two public listings. Both read this service's own projection, which is the whole point of
/// splitting the sales service out: listing traffic never touches the catalogue's database.
/// </summary>
[Route("api/vehicles")]
[Produces("application/json")]
public sealed class VehicleListingsController(
    IUseCase<ListVehiclesForSaleQuery, PagedList<VehicleListingResponse>> listForSale,
    IUseCase<ListSoldVehiclesQuery, PagedList<VehicleListingResponse>> listSold) : ApiControllerBase
{
    /// <summary>Vehicles available for sale, cheapest first.</summary>
    [HttpGet("for-sale")]
    [ProducesResponseType<PagedList<VehicleListingResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<VehicleListingResponse>>> ForSale(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        FromResult(await listForSale.ExecuteAsync(new ListVehiclesForSaleQuery(page, pageSize), cancellationToken));

    /// <summary>Vehicles already sold, cheapest first.</summary>
    [HttpGet("sold")]
    [ProducesResponseType<PagedList<VehicleListingResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<VehicleListingResponse>>> Sold(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        FromResult(await listSold.ExecuteAsync(new ListSoldVehiclesQuery(page, pageSize), cancellationToken));
}
