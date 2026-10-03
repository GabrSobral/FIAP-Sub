namespace FiapSub.Core.Application.Common;

/// <summary>Stable machine-readable error codes returned in the ProblemDetails payload.</summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "validation.failed";
    public const string VehicleNotFound = "vehicle.not_found";
    public const string PaymentVehicleNotFound = "payment.vehicle_not_found";
}
