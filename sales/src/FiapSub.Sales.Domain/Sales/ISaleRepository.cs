using FiapSub.Sales.Domain.ValueObjects;

namespace FiapSub.Sales.Domain.Sales;

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(Guid saleId, CancellationToken cancellationToken);

    Task<Sale?> GetByPaymentCodeAsync(PaymentCode paymentCode, CancellationToken cancellationToken);

    /// <summary>
    /// The open purchase for a vehicle, if any. Guards against two buyers racing for the same car;
    /// a unique index backs the same rule in the database.
    /// </summary>
    Task<Sale?> GetPendingByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    void Add(Sale sale);
}
