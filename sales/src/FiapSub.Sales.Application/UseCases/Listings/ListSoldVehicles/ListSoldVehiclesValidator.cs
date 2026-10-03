using FluentValidation;

namespace FiapSub.Sales.Application.UseCases.Listings.ListSoldVehicles;

public sealed class ListSoldVehiclesValidator : AbstractValidator<ListSoldVehiclesQuery>
{
    public ListSoldVehiclesValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(VehicleListingQuery.MaxPageSize);
    }
}
