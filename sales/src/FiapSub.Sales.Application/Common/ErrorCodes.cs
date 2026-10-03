namespace FiapSub.Sales.Application.Common;

/// <summary>Stable machine-readable error codes returned in the ProblemDetails payload.</summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "validation.failed";
    public const string VehicleNotInCatalogue = "vehicle.not_in_catalogue";
    public const string VehicleNotAvailable = "vehicle.not_available";
    public const string SaleAlreadyPending = "sale.already_pending";
    public const string SaleNotFound = "sale.not_found";
    public const string CatalogueUnavailable = "catalogue.unavailable";
}
