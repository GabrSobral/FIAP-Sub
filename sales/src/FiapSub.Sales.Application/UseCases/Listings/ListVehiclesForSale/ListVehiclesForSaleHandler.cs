using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Contracts;
using FiapSub.Sales.Domain.Catalog;

namespace FiapSub.Sales.Application.UseCases.Listings.ListVehiclesForSale;

public sealed class ListVehiclesForSaleHandler(IVehicleSnapshotRepository snapshots)
    : IUseCase<ListVehiclesForSaleQuery, PagedList<VehicleListingResponse>>
{
    public Task<Result<PagedList<VehicleListingResponse>>> ExecuteAsync(
        ListVehiclesForSaleQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return VehicleListingReader.ReadAsync(
            snapshots,
            VehicleAvailability.Available,
            new VehicleListingQuery(request.Page, request.PageSize),
            cancellationToken);
    }
}
