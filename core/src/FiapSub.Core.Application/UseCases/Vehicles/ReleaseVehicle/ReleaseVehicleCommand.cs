namespace FiapSub.Core.Application.UseCases.Vehicles.ReleaseVehicle;

/// <summary>
/// Compensating action: the sales service could not complete the purchase, so the hold on the
/// vehicle must be dropped.
/// </summary>
public sealed record ReleaseVehicleCommand(Guid VehicleId, string? PaymentCode);
