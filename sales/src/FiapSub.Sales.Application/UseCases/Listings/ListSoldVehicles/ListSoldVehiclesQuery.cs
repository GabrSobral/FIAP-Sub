namespace FiapSub.Sales.Application.UseCases.Listings.ListSoldVehicles;

/// <summary>Vehicles already sold, cheapest first.</summary>
public sealed record ListSoldVehiclesQuery(int Page = 1, int PageSize = 20);
