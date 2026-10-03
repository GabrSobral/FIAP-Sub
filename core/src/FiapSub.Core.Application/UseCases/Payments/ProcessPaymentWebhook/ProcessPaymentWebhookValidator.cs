using FluentValidation;
using FiapSub.Core.Domain.ValueObjects;

namespace FiapSub.Core.Application.UseCases.Payments.ProcessPaymentWebhook;

public sealed class ProcessPaymentWebhookValidator : AbstractValidator<ProcessPaymentWebhookCommand>
{
    public ProcessPaymentWebhookValidator()
    {
        RuleFor(x => x.PaymentCode).NotEmpty().MaximumLength(PaymentCode.MaxLength);
        RuleFor(x => x.Outcome).IsInEnum();
    }
}
