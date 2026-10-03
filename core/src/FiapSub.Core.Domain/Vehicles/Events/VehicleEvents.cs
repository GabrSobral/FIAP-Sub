namespace FiapSub.Core.Domain.Vehicles.Events;

/// <summary>A vehicle entered the catalogue and is available for sale.</summary>
public sealed record VehicleRegistered(Guid VehicleId, DateTimeOffset OccurredOn) : IVehicleProjectionEvent;

/// <summary>The descriptive data or the price of a vehicle changed.</summary>
public sealed record VehicleDetailsUpdated(Guid VehicleId, DateTimeOffset OccurredOn) : IVehicleProjectionEvent;

/// <summary>A purchase was started for this vehicle and it is awaiting payment.</summary>
public sealed record VehicleReserved(Guid VehicleId, string PaymentCode, DateTimeOffset OccurredOn) : IVehicleProjectionEvent;

/// <summary>The payment settled and the vehicle changed hands.</summary>
public sealed record VehicleSold(Guid VehicleId, string PaymentCode, DateTimeOffset OccurredOn) : IVehicleProjectionEvent;

/// <summary>The payment was cancelled, so the vehicle is back on the shelf.</summary>
public sealed record VehicleReservationReleased(Guid VehicleId, string PaymentCode, DateTimeOffset OccurredOn) : IVehicleProjectionEvent;
