using FluentValidation;
using FiapSub.Core.Domain.ValueObjects;

namespace FiapSub.Core.Application.UseCases.Vehicles.ReleaseVehicle;

public sealed class ReleaseVehicleValidator : AbstractValidator<ReleaseVehicleCommand>
{
    public ReleaseVehicleValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.PaymentCode).NotEmpty().MaximumLength(PaymentCode.MaxLength);
    }
}
