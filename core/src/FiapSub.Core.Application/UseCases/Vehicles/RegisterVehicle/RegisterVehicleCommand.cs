namespace FiapSub.Core.Application.UseCases.Vehicles.RegisterVehicle;

/// <summary>Puts a new vehicle up for sale.</summary>
public sealed record RegisterVehicleCommand(
    string? Brand,
    string? Model,
    int Year,
    string? Color,
    decimal Price);
