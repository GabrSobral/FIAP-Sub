namespace FiapSub.Sales.Application.Gateways;

/// <summary>
/// Outbound HTTP contract towards the catalogue service, which remains the only authority on a
/// vehicle's status.
/// </summary>
public interface IVehicleCatalogueGateway
{
    /// <summary>Asks the catalogue to hold a vehicle against a payment code.</summary>
    Task<CatalogueReservation> ReserveAsync(Guid vehicleId, string paymentCode, CancellationToken cancellationToken);

    /// <summary>
    /// Best-effort compensation after an ambiguous reserve: drops a hold that may have been
    /// placed. Returns false instead of throwing, because the caller is already on a failure path.
    /// </summary>
    Task<bool> TryReleaseAsync(Guid vehicleId, string paymentCode, CancellationToken cancellationToken);

    /// <summary>One page of the catalogue, used to rebuild or repair the local projection.</summary>
    Task<IReadOnlyList<CatalogueVehicle>> ExportAsync(int page, int pageSize, CancellationToken cancellationToken);
}
