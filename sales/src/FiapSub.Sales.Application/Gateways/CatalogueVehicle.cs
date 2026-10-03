namespace FiapSub.Sales.Application.Gateways;

/// <summary>
/// A vehicle as the catalogue service describes it. Mirrors the payload the catalogue pushes and
/// the one its export endpoint returns, so both paths feed the same projection code.
/// </summary>
public sealed record CatalogueVehicle(
    Guid VehicleId,
    string? Brand,
    string? Model,
    int Year,
    string? Color,
    decimal Price,
    string? Currency,
    string? Status,
    long Version,
    DateTimeOffset UpdatedAt);
