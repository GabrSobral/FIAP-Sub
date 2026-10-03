using FiapSub.Sales.Domain.Abstractions;

namespace FiapSub.Sales.Domain.Sales.Events;

/// <summary>A buyer started a purchase; the vehicle is being held by the catalogue service.</summary>
public sealed record SaleOpened(Guid SaleId, Guid VehicleId, string PaymentCode, DateTimeOffset OccurredOn)
    : IDomainEvent;

/// <summary>The payment settled and the sale is closed.</summary>
public sealed record SalePaid(Guid SaleId, Guid VehicleId, DateTimeOffset SaleDate, DateTimeOffset OccurredOn)
    : IDomainEvent;

/// <summary>The purchase did not go through.</summary>
public sealed record SaleCancelled(Guid SaleId, Guid VehicleId, string Reason, DateTimeOffset OccurredOn)
    : IDomainEvent;
