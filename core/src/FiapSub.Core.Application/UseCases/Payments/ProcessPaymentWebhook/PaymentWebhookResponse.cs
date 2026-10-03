namespace FiapSub.Core.Application.UseCases.Payments.ProcessPaymentWebhook;

/// <param name="AlreadyProcessed">
/// True when this payment code had already been notified, i.e. the call was a replay and
/// changed nothing.
/// </param>
public sealed record PaymentWebhookResponse(
    string PaymentCode,
    string Outcome,
    bool AlreadyProcessed,
    Guid? VehicleId,
    string? VehicleStatus);
