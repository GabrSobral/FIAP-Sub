namespace FiapSub.Sales.Application.UseCases.Catalogue.UpsertVehicleSnapshot;

/// <summary>
/// A vehicle as pushed by the catalogue service. <paramref name="Version"/> is what makes the
/// endpoint safe to call repeatedly and out of order.
/// </summary>
public sealed record UpsertVehicleSnapshotCommand(
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
