using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Domain.Catalog;

namespace FiapSub.Sales.Application.UseCases.Catalogue.UpsertVehicleSnapshot;

/// <summary>
/// Writes the catalogue's view of a vehicle into the local projection. Idempotent by version, so
/// the catalogue is free to retry its pushes.
/// </summary>
public sealed class UpsertVehicleSnapshotHandler(
    IVehicleSnapshotRepository snapshots,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IUseCase<UpsertVehicleSnapshotCommand, VehicleSnapshotAck>
{
    public async Task<Result<VehicleSnapshotAck>> ExecuteAsync(
        UpsertVehicleSnapshotCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.TryParse<VehicleAvailability>(request.Status, ignoreCase: true, out var status)
            || !Enum.IsDefined(status))
        {
            return Result.Failure<VehicleSnapshotAck>(Error.Validation(
                ErrorCodes.ValidationFailed,
                $"Unknown vehicle status '{request.Status}'."));
        }

        var now = clock.UtcNow;
        var existing = await snapshots.GetByIdAsync(request.VehicleId, cancellationToken);

        if (existing is null)
        {
            var snapshot = VehicleSnapshot.Replicate(
                request.VehicleId,
                request.Brand,
                request.Model,
                request.Year,
                request.Color,
                request.Price,
                request.Currency,
                status,
                request.Version,
                request.UpdatedAt,
                now);

            snapshots.Add(snapshot);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new VehicleSnapshotAck(
                snapshot.Id, snapshot.Version, Applied: true, snapshot.Status.ToString()));
        }

        var applied = existing.TryApplyUpdate(
            request.Brand,
            request.Model,
            request.Year,
            request.Color,
            request.Price,
            request.Currency,
            status,
            request.Version,
            request.UpdatedAt,
            now);

        if (applied)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(new VehicleSnapshotAck(
            existing.Id, existing.Version, applied, existing.Status.ToString()));
    }
}
