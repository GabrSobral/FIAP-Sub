using FluentValidation;
using FiapSub.Sales.Domain.ValueObjects;

namespace FiapSub.Sales.Application.UseCases.Sales.OpenSale;

public sealed class OpenSaleValidator : AbstractValidator<OpenSaleCommand>
{
    public OpenSaleValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.BuyerCpf)
            .NotEmpty()
            .Must(cpf => cpf is not null && cpf.Count(char.IsAsciiDigit) == Cpf.Length)
            .WithMessage($"'BuyerCpf' must contain {Cpf.Length} digits.");
    }
}
