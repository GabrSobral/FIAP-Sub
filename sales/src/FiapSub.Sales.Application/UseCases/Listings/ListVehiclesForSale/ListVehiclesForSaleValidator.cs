using FluentValidation;

namespace FiapSub.Sales.Application.UseCases.Listings.ListVehiclesForSale;

public sealed class ListVehiclesForSaleValidator : AbstractValidator<ListVehiclesForSaleQuery>
{
    public ListVehiclesForSaleValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(VehicleListingQuery.MaxPageSize);
    }
}
