namespace FiapSub.Core.Application.UseCases.Vehicles.UpdateVehicle;

/// <summary>Replaces the editable data of a vehicle that is still available.</summary>
public sealed record UpdateVehicleCommand(
    Guid VehicleId,
    string? Brand,
    string? Model,
    int Year,
    string? Color,
    decimal Price);
