using FiapSub.Core.Domain.Abstractions;

namespace FiapSub.Core.Domain.Vehicles.Events;

/// <summary>
/// Marks the events whose occurrence means the sales service's read model is now stale.
/// A single handler reacts to all of them by republishing the vehicle.
/// </summary>
public interface IVehicleProjectionEvent : IDomainEvent
{
    Guid VehicleId { get; }
}
