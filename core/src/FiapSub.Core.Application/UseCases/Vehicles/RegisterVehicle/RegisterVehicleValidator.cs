using FluentValidation;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.UseCases.Vehicles.RegisterVehicle;

public sealed class RegisterVehicleValidator : AbstractValidator<RegisterVehicleCommand>
{
    public RegisterVehicleValidator()
    {
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(Vehicle.MaxBrandLength);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(Vehicle.MaxModelLength);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(Vehicle.MaxColorLength);
        RuleFor(x => x.Year).GreaterThanOrEqualTo(1900);
        RuleFor(x => x.Price).GreaterThan(0m);
    }
}
