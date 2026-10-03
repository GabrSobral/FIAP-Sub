using FiapSub.Core.Domain.ValueObjects;
using FiapSub.Core.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace FiapSub.Core.Infrastructure.Database.Repositories;

internal sealed class VehicleRepository(CoreDbContext context) : IVehicleRepository
{
    /// <summary>Tracked: callers of this overload go on to mutate the aggregate.</summary>
    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<Vehicle?> GetByReservationPaymentCodeAsync(
        PaymentCode paymentCode,
        CancellationToken cancellationToken) =>
        context.Vehicles.FirstOrDefaultAsync(v => v.ReservationPaymentCode == paymentCode, cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> ListAsync(
        VehicleStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        await context.Vehicles
            .AsNoTracking()
            .Where(v => status == null || v.Status == status)
            .OrderBy(v => v.CreatedAt)
            .ThenBy(v => v.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(VehicleStatus? status, CancellationToken cancellationToken) =>
        context.Vehicles.CountAsync(v => status == null || v.Status == status, cancellationToken);

    public void Add(Vehicle vehicle) => context.Vehicles.Add(vehicle);
}
