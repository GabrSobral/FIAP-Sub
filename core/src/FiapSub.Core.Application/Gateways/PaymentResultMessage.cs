namespace FiapSub.Core.Application.Gateways;

/// <summary>
/// Tells the sales service how a payment ended, so it can close or cancel the matching sale.
/// </summary>
/// <param name="Status">Either <c>approved</c> or <c>cancelled</c>.</param>
public sealed record PaymentResultMessage(
    string PaymentCode,
    string Status,
    Guid VehicleId,
    DateTimeOffset OccurredOn)
{
    public const string Approved = "approved";
    public const string Cancelled = "cancelled";
}
