using FluentValidation;

namespace FiapSub.Sales.Application.UseCases.Catalogue.UpsertVehicleSnapshot;

public sealed class UpsertVehicleSnapshotValidator : AbstractValidator<UpsertVehicleSnapshotCommand>
{
    public UpsertVehicleSnapshotValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Brand).NotEmpty();
        RuleFor(x => x.Model).NotEmpty();
        RuleFor(x => x.Color).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0m);
        RuleFor(x => x.Version).GreaterThan(0);
    }
}
