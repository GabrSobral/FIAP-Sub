namespace FiapSub.Sales.Domain.Catalog;

public interface IVehicleSnapshotRepository
{
    Task<VehicleSnapshot?> GetByIdAsync(Guid vehicleId, CancellationToken cancellationToken);

    /// <summary>Price ascending, as both public listings require.</summary>
    Task<IReadOnlyList<VehicleSnapshot>> ListByStatusOrderedByPriceAsync(
        VehicleAvailability status,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<int> CountByStatusAsync(VehicleAvailability status, CancellationToken cancellationToken);

    void Add(VehicleSnapshot snapshot);
}
