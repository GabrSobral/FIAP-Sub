using FiapSub.Sales.Domain.Catalog;

namespace FiapSub.Sales.Application.Contracts;

/// <summary>One row of the public vehicle listings.</summary>
public sealed record VehicleListingResponse(
    Guid VehicleId,
    string Brand,
    string Model,
    int Year,
    string Color,
    decimal Price,
    string Currency,
    string Status,
    DateTimeOffset UpdatedAt)
{
    public static VehicleListingResponse From(VehicleSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new VehicleListingResponse(
            snapshot.Id,
            snapshot.Brand,
            snapshot.Model,
            snapshot.Year,
            snapshot.Color,
            snapshot.Price,
            snapshot.Currency,
            snapshot.Status.ToString(),
            snapshot.UpdatedAt);
    }
}
