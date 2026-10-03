namespace FiapSub.Sales.Domain.Sales;

/// <summary>Where a purchase stands.</summary>
public enum SaleStatus
{
    /// <summary>Opened and waiting for the payment provider's verdict.</summary>
    PendingPayment = 1,

    /// <summary>Payment approved; the vehicle changed hands. Terminal state.</summary>
    Paid = 2,

    /// <summary>Payment refused, or the purchase could not be completed. Terminal state.</summary>
    Cancelled = 3,
}
