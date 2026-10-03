namespace FiapSub.Sales.Application.Gateways;

/// <summary>Why a hold on a vehicle did or did not succeed.</summary>
public enum CatalogueReservationOutcome
{
    /// <summary>The catalogue is holding the vehicle for this payment code.</summary>
    Reserved = 1,

    /// <summary>The catalogue has no such vehicle; our projection is out of date.</summary>
    VehicleNotFound = 2,

    /// <summary>Someone else is already buying it, or it is sold.</summary>
    VehicleUnavailable = 3,

    /// <summary>
    /// We could not get an answer. The hold may or may not have been placed, so the caller has to
    /// assume it was and compensate.
    /// </summary>
    CatalogueUnavailable = 4,
}

/// <inheritdoc cref="CatalogueReservationOutcome"/>
public sealed record CatalogueReservation(CatalogueReservationOutcome Outcome, string? Detail)
{
    public bool Succeeded => Outcome == CatalogueReservationOutcome.Reserved;

    public static CatalogueReservation Reserved() => new(CatalogueReservationOutcome.Reserved, null);

    public static CatalogueReservation VehicleNotFound(string? detail) =>
        new(CatalogueReservationOutcome.VehicleNotFound, detail);

    public static CatalogueReservation VehicleUnavailable(string? detail) =>
        new(CatalogueReservationOutcome.VehicleUnavailable, detail);

    public static CatalogueReservation CatalogueUnavailable(string? detail) =>
        new(CatalogueReservationOutcome.CatalogueUnavailable, detail);
}
