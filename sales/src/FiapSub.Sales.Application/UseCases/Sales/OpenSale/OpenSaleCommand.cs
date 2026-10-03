namespace FiapSub.Sales.Application.UseCases.Sales.OpenSale;

/// <summary>
/// Starts a purchase: who is buying (CPF) and, optionally, the date to record as the sale date.
/// When omitted, the sale date is the instant the payment is approved.
/// </summary>
public sealed record OpenSaleCommand(Guid VehicleId, string? BuyerCpf, DateTimeOffset? SaleDate);
