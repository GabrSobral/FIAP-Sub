using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Contracts;
using FiapSub.Sales.Domain.Catalog;

namespace FiapSub.Sales.Application.UseCases.Listings.ListSoldVehicles;

public sealed class ListSoldVehiclesHandler(IVehicleSnapshotRepository snapshots)
    : IUseCase<ListSoldVehiclesQuery, PagedList<VehicleListingResponse>>
{
    public Task<Result<PagedList<VehicleListingResponse>>> ExecuteAsync(
        ListSoldVehiclesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return VehicleListingReader.ReadAsync(
            snapshots,
            VehicleAvailability.Sold,
            new VehicleListingQuery(request.Page, request.PageSize),
            cancellationToken);
    }
}
