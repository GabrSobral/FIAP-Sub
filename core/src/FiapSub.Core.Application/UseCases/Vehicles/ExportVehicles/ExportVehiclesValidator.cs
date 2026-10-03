using FluentValidation;

namespace FiapSub.Core.Application.UseCases.Vehicles.ExportVehicles;

public sealed class ExportVehiclesValidator : AbstractValidator<ExportVehiclesQuery>
{
    public ExportVehiclesValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(ExportVehiclesQuery.MaxPageSize);
    }
}
