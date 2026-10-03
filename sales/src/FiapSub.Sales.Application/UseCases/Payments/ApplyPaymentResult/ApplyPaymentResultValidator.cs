using FluentValidation;
using FiapSub.Sales.Domain.ValueObjects;

namespace FiapSub.Sales.Application.UseCases.Payments.ApplyPaymentResult;

public sealed class ApplyPaymentResultValidator : AbstractValidator<ApplyPaymentResultCommand>
{
    public ApplyPaymentResultValidator()
    {
        RuleFor(x => x.PaymentCode).NotEmpty().MaximumLength(PaymentCode.MaxLength);
        RuleFor(x => x.Status).NotEmpty();
    }
}
