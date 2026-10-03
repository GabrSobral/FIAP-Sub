using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Gateways;
using FiapSub.Sales.Application.UseCases.Catalogue.UpsertVehicleSnapshot;
using Microsoft.Extensions.Logging;

namespace FiapSub.Sales.Application.UseCases.Catalogue.SyncCatalogue;

public sealed class SyncCatalogueHandler(
    IVehicleCatalogueGateway catalogue,
    IUseCase<UpsertVehicleSnapshotCommand, VehicleSnapshotAck> upsert,
    ILogger<SyncCatalogueHandler> logger) : IUseCase<SyncCatalogueCommand, SyncCatalogueResult>
{
    public async Task<Result<SyncCatalogueResult>> ExecuteAsync(
        SyncCatalogueCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pulled = 0;
        var applied = 0;

        for (var page = 1; page <= SyncCatalogueCommand.MaxPages; page++)
        {
            var vehicles = await catalogue.ExportAsync(page, request.PageSize, cancellationToken);

            foreach (var vehicle in vehicles)
            {
                pulled++;

                var result = await upsert.ExecuteAsync(
                    new UpsertVehicleSnapshotCommand(
                        vehicle.VehicleId,
                        vehicle.Brand,
                        vehicle.Model,
                        vehicle.Year,
                        vehicle.Color,
                        vehicle.Price,
                        vehicle.Currency,
                        vehicle.Status,
                        vehicle.Version,
                        vehicle.UpdatedAt),
                    cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogWarning(
                        "Skipped vehicle {VehicleId} while syncing the catalogue: {Reason}",
                        vehicle.VehicleId,
                        result.Error.Message);

                    continue;
                }

                if (result.Value.Applied)
                {
                    applied++;
                }
            }

            if (vehicles.Count < request.PageSize)
            {
                break;
            }
        }

        logger.LogInformation("Catalogue sync pulled {Pulled} vehicles and applied {Applied}.", pulled, applied);

        return Result.Success(new SyncCatalogueResult(pulled, applied));
    }
}
