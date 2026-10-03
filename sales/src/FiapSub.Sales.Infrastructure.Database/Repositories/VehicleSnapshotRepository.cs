using FiapSub.Sales.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace FiapSub.Sales.Infrastructure.Database.Repositories;

internal sealed class VehicleSnapshotRepository(SalesDbContext context) : IVehicleSnapshotRepository
{
    public Task<VehicleSnapshot?> GetByIdAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        context.VehicleSnapshots.FirstOrDefaultAsync(snapshot => snapshot.Id == vehicleId, cancellationToken);

    /// <summary>
    /// The query the whole service exists for. Backed by the (status, price) index, so a spike in
    /// listing traffic stays a cheap index scan in this service's own database.
    /// </summary>
    public async Task<IReadOnlyList<VehicleSnapshot>> ListByStatusOrderedByPriceAsync(
        VehicleAvailability status,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        await context.VehicleSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.Status == status)
            .OrderBy(snapshot => snapshot.Price)
            .ThenBy(snapshot => snapshot.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountByStatusAsync(VehicleAvailability status, CancellationToken cancellationToken) =>
        context.VehicleSnapshots.CountAsync(snapshot => snapshot.Status == status, cancellationToken);

    public void Add(VehicleSnapshot snapshot) => context.VehicleSnapshots.Add(snapshot);
}
