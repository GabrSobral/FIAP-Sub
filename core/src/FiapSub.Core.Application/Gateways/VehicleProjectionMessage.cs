using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.Gateways;

/// <summary>
/// The slice of a vehicle the sales service replicates so it can serve the public listings
/// without calling back here. <see cref="Version"/> lets the receiver drop stale updates.
/// </summary>
public sealed record VehicleProjectionMessage(
    Guid VehicleId,
    string Brand,
    string Model,
    int Year,
    string Color,
    decimal Price,
    string Currency,
    string Status,
    long Version,
    DateTimeOffset UpdatedAt)
{
    public static VehicleProjectionMessage From(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        return new VehicleProjectionMessage(
            vehicle.Id,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Year.Value,
            vehicle.Color,
            vehicle.Price.Amount,
            vehicle.Price.Currency,
            vehicle.Status.ToString(),
            vehicle.Version,
            vehicle.UpdatedAt);
    }
}
