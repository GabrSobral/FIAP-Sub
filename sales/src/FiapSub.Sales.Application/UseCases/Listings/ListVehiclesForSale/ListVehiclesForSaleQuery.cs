namespace FiapSub.Sales.Application.UseCases.Listings.ListVehiclesForSale;

/// <summary>Vehicles still on sale, cheapest first.</summary>
public sealed record ListVehiclesForSaleQuery(int Page = 1, int PageSize = 20);
