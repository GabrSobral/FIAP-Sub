using FiapSub.Core.Api.Contracts;
using FiapSub.Core.Api.Infrastructure;
using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.Contracts;
using FiapSub.Core.Application.Gateways;
using FiapSub.Core.Application.UseCases.Vehicles.ExportVehicles;
using FiapSub.Core.Application.UseCases.Vehicles.ReleaseVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.ReserveVehicle;

namespace FiapSub.Core.Api.Controllers;

/// <summary>
/// Service-to-service surface used by the sales service. Guarded by the shared internal token
/// and never exposed through the public ingress.
/// </summary>
[Route("api/internal/vehicles")]
[Produces("application/json")]
public sealed class InternalVehiclesController(
    IUseCase<ReserveVehicleCommand, VehicleResponse> reserveVehicle,
    IUseCase<ReleaseVehicleCommand, VehicleResponse> releaseVehicle,
    IUseCase<ExportVehiclesQuery, PagedList<VehicleProjectionMessage>> exportVehicles) : ApiControllerBase
{
    /// <summary>Holds a vehicle for a purchase the sales service has just opened.</summary>
    [HttpPost("{id:guid}/reserve")]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleResponse>> Reserve(
        Guid id,
        [FromBody] PaymentCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return FromResult(await reserveVehicle.ExecuteAsync(
            new ReserveVehicleCommand(id, request.PaymentCode),
            cancellationToken));
    }

    /// <summary>Drops a hold the sales service could not turn into a sale.</summary>
    [HttpPost("{id:guid}/release")]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleResponse>> Release(
        Guid id,
        [FromBody] PaymentCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return FromResult(await releaseVehicle.ExecuteAsync(
            new ReleaseVehicleCommand(id, request.PaymentCode),
            cancellationToken));
    }

    /// <summary>
    /// Full catalogue dump so the sales service can rebuild its projection, which is how any
    /// push lost to a transient failure is reconciled.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedList<VehicleProjectionMessage>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedList<VehicleProjectionMessage>>> Export(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default) =>
        FromResult(await exportVehicles.ExecuteAsync(new ExportVehiclesQuery(page, pageSize), cancellationToken));
}
