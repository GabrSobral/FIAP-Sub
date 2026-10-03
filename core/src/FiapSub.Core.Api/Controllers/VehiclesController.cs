using FiapSub.Core.Api.Contracts;
using FiapSub.Core.Api.Infrastructure;
using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.Contracts;
using FiapSub.Core.Application.UseCases.Vehicles.GetVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.ListVehicles;
using FiapSub.Core.Application.UseCases.Vehicles.RegisterVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.UpdateVehicle;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Api.Controllers;

/// <summary>
/// The vehicle catalogue. This service owns the vehicle data; the price-ordered listings the
/// public hits live in the sales service.
/// </summary>
[Route("api/vehicles")]
[Produces("application/json")]
public sealed class VehiclesController(
    IUseCase<RegisterVehicleCommand, VehicleResponse> registerVehicle,
    IUseCase<UpdateVehicleCommand, VehicleResponse> updateVehicle,
    IUseCase<GetVehicleQuery, VehicleResponse> getVehicle,
    IUseCase<ListVehiclesQuery, PagedList<VehicleResponse>> listVehicles) : ApiControllerBase
{
    /// <summary>Registers a vehicle for sale.</summary>
    [HttpPost]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VehicleResponse>> Register(
        [FromBody] RegisterVehicleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await registerVehicle.ExecuteAsync(
            new RegisterVehicleCommand(request.Brand, request.Model, request.Year, request.Color, request.Price),
            cancellationToken);

        return FromResult(result, vehicle => CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle));
    }

    /// <summary>Replaces the editable data of a vehicle that is still available.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleResponse>> Update(
        Guid id,
        [FromBody] UpdateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await updateVehicle.ExecuteAsync(
            new UpdateVehicleCommand(id, request.Brand, request.Model, request.Year, request.Color, request.Price),
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>Fetches a single vehicle.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        FromResult(await getVehicle.ExecuteAsync(new GetVehicleQuery(id), cancellationToken));

    /// <summary>Operational listing of the catalogue, optionally filtered by status.</summary>
    [HttpGet]
    [ProducesResponseType<PagedList<VehicleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<VehicleResponse>>> List(
        [FromQuery] VehicleStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        FromResult(await listVehicles.ExecuteAsync(new ListVehiclesQuery(status, page, pageSize), cancellationToken));
}
