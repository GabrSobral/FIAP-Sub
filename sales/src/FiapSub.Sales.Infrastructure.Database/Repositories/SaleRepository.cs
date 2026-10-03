using FiapSub.Sales.Domain.Sales;
using FiapSub.Sales.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FiapSub.Sales.Infrastructure.Database.Repositories;

internal sealed class SaleRepository(SalesDbContext context) : ISaleRepository
{
    public Task<Sale?> GetByIdAsync(Guid saleId, CancellationToken cancellationToken) =>
        context.Sales.FirstOrDefaultAsync(sale => sale.Id == saleId, cancellationToken);

    public Task<Sale?> GetByPaymentCodeAsync(PaymentCode paymentCode, CancellationToken cancellationToken) =>
        context.Sales.FirstOrDefaultAsync(sale => sale.PaymentCode == paymentCode, cancellationToken);

    public Task<Sale?> GetPendingByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        context.Sales.FirstOrDefaultAsync(
            sale => sale.VehicleId == vehicleId && sale.Status == SaleStatus.PendingPayment,
            cancellationToken);

    public void Add(Sale sale) => context.Sales.Add(sale);
}
