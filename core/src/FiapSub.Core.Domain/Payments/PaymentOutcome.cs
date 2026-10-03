namespace FiapSub.Core.Domain.Payments;

/// <summary>What the payment provider told us about a transaction.</summary>
public enum PaymentOutcome
{
    /// <summary>The money came through; the sale can be completed.</summary>
    Approved = 1,

    /// <summary>The payment fell through; the vehicle must go back on the shelf.</summary>
    Cancelled = 2,
}
