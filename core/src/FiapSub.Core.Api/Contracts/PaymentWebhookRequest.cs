using FiapSub.Core.Domain.Payments;

namespace FiapSub.Core.Api.Contracts;

/// <summary>
/// Body of <c>POST /api/webhooks/payments</c>, as sent by the payment provider.
/// </summary>
/// <param name="Status">Accepted values: <c>approved</c> and <c>cancelled</c>.</param>
public sealed record PaymentWebhookRequest(string? PaymentCode, string? Status)
{
    /// <summary>
    /// Maps the provider's wording onto the domain enum. Returns false for anything we do not
    /// understand, which the controller reports as a 400 instead of guessing.
    /// </summary>
    public bool TryParseOutcome(out PaymentOutcome outcome)
    {
        switch (Status?.Trim().ToUpperInvariant())
        {
            case "APPROVED":
            case "PAID":
            case "APROVADO":
                outcome = PaymentOutcome.Approved;
                return true;

            case "CANCELLED":
            case "CANCELED":
            case "REJECTED":
            case "CANCELADO":
                outcome = PaymentOutcome.Cancelled;
                return true;

            default:
                outcome = default;
                return false;
        }
    }
}
