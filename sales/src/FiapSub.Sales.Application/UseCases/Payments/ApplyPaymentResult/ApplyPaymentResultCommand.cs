namespace FiapSub.Sales.Application.UseCases.Payments.ApplyPaymentResult;

/// <summary>
/// How a payment ended, as reported by the catalogue service after it received the provider's
/// webhook. The catalogue owns the vehicle; this service owns the sale.
/// </summary>
/// <param name="Status">Either <c>approved</c> or <c>cancelled</c>.</param>
public sealed record ApplyPaymentResultCommand(string? PaymentCode, string? Status)
{
    public const string Approved = "approved";
    public const string Cancelled = "cancelled";

    public bool TryParseApproved(out bool approved)
    {
        switch (Status?.Trim().ToUpperInvariant())
        {
            case "APPROVED":
            case "PAID":
                approved = true;
                return true;

            case "CANCELLED":
            case "CANCELED":
            case "REJECTED":
                approved = false;
                return true;

            default:
                approved = false;
                return false;
        }
    }
}
