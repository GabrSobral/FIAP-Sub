using FluentValidation;
using FiapSub.Core.Domain.ValueObjects;

namespace FiapSub.Core.Application.UseCases.Vehicles.ReserveVehicle;

public sealed class ReserveVehicleValidator : AbstractValidator<ReserveVehicleCommand>
{
    public ReserveVehicleValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.PaymentCode).NotEmpty().MaximumLength(PaymentCode.MaxLength);
    }
}
