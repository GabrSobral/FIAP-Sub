using FluentValidation;

namespace FiapSub.Core.Application.UseCases.Vehicles.ListVehicles;

public sealed class ListVehiclesValidator : AbstractValidator<ListVehiclesQuery>
{
    public ListVehiclesValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(ListVehiclesQuery.MaxPageSize);
    }
}
