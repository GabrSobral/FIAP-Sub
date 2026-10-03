using FiapSub.Core.Domain.Payments;

namespace FiapSub.Core.Application.UseCases.Payments.ProcessPaymentWebhook;

/// <summary>
/// What the payment provider sends us: a payment code and whether the money went through.
/// The provider knows nothing about vehicles, so the payment code is the only link we have.
/// </summary>
public sealed record ProcessPaymentWebhookCommand(string? PaymentCode, PaymentOutcome Outcome);
