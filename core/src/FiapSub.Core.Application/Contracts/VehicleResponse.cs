using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.Contracts;

/// <summary>What the catalogue API returns for a single vehicle.</summary>
public sealed record VehicleResponse(
    Guid Id,
    string Brand,
    string Model,
    int Year,
    string Color,
    decimal Price,
    string Currency,
    string Status,
    string? PaymentCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version)
{
    public static VehicleResponse From(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        return new VehicleResponse(
            vehicle.Id,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Year.Value,
            vehicle.Color,
            vehicle.Price.Amount,
            vehicle.Price.Currency,
            vehicle.Status.ToString(),
            vehicle.ReservationPaymentCode?.Value,
            vehicle.CreatedAt,
            vehicle.UpdatedAt,
            vehicle.Version);
    }
}
