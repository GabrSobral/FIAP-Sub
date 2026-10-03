using FiapSub.Core.Domain.ValueObjects;

namespace FiapSub.Core.Domain.Vehicles;

/// <summary>
/// The catalogue's persistence contract. Declared here so the domain states what it needs and
/// the infrastructure layer supplies it.
/// </summary>
public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Vehicle?> GetByReservationPaymentCodeAsync(PaymentCode paymentCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<Vehicle>> ListAsync(VehicleStatus? status, int skip, int take, CancellationToken cancellationToken);

    Task<int> CountAsync(VehicleStatus? status, CancellationToken cancellationToken);

    void Add(Vehicle vehicle);
}
