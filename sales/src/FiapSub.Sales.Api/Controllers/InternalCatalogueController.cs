using FiapSub.Sales.Api.Contracts;
using FiapSub.Sales.Api.Infrastructure;
using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.UseCases.Catalogue.SyncCatalogue;
using FiapSub.Sales.Application.UseCases.Catalogue.UpsertVehicleSnapshot;

namespace FiapSub.Sales.Api.Controllers;

/// <summary>
/// Service-to-service surface the catalogue pushes into. Guarded by the shared internal token
/// and never exposed through the public ingress.
/// </summary>
[Route("api/internal")]
[Produces("application/json")]
public sealed class InternalCatalogueController(
    IUseCase<UpsertVehicleSnapshotCommand, VehicleSnapshotAck> upsertSnapshot,
    IUseCase<SyncCatalogueCommand, SyncCatalogueResult> syncCatalogue) : ApiControllerBase
{
    /// <summary>
    /// Replicates one vehicle. Idempotent by version, so the catalogue may retry and may arrive
    /// out of order without corrupting the listings.
    /// </summary>
    [HttpPut("vehicles/{id:guid}")]
    [ProducesResponseType<VehicleSnapshotAck>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VehicleSnapshotAck>> UpsertVehicle(
        Guid id,
        [FromBody] VehicleProjectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await upsertSnapshot.ExecuteAsync(
            new UpsertVehicleSnapshotCommand(
                id,
                request.Brand,
                request.Model,
                request.Year,
                request.Color,
                request.Price,
                request.Currency,
                request.Status,
                request.Version,
                request.UpdatedAt),
            cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Pulls the whole catalogue and replays it into the projection. Used to fill a fresh
    /// deployment and to reconcile anything a push lost.
    /// </summary>
    [HttpPost("catalogue/sync")]
    [ProducesResponseType<SyncCatalogueResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SyncCatalogueResult>> Sync(
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default) =>
        FromResult(await syncCatalogue.ExecuteAsync(new SyncCatalogueCommand(pageSize), cancellationToken));
}
