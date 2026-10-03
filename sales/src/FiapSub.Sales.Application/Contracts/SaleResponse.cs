using FiapSub.Sales.Domain.Sales;

namespace FiapSub.Sales.Application.Contracts;

/// <summary>
/// A purchase as returned by the API. The buyer's CPF is masked rather than raw, since it is
/// personal data and nothing downstream needs the bare digits.
/// </summary>
public sealed record SaleResponse(
    Guid Id,
    Guid VehicleId,
    string BuyerCpf,
    decimal Amount,
    string Currency,
    string PaymentCode,
    string Status,
    DateTimeOffset? SaleDate,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version)
{
    public static SaleResponse From(Sale sale)
    {
        ArgumentNullException.ThrowIfNull(sale);

        return new SaleResponse(
            sale.Id,
            sale.VehicleId,
            sale.BuyerCpf.Masked,
            sale.Amount.Amount,
            sale.Amount.Currency,
            sale.PaymentCode.Value,
            sale.Status.ToString(),
            sale.SaleDate,
            sale.CancellationReason,
            sale.CreatedAt,
            sale.UpdatedAt,
            sale.Version);
    }
}
