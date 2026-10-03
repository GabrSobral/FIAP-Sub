namespace FiapSub.Core.Application.UseCases.Vehicles.ReserveVehicle;

/// <summary>
/// Called by the sales service when a buyer starts a purchase. Holds the vehicle against the
/// payment code the sales service generated.
/// </summary>
public sealed record ReserveVehicleCommand(Guid VehicleId, string? PaymentCode);
