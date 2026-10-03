namespace FiapSub.Sales.Application.UseCases.Listings;

/// <summary>Paging for the public listings. Ordering is fixed: price ascending.</summary>
public sealed record VehicleListingQuery(int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;
}
