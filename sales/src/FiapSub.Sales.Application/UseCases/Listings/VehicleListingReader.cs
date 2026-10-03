using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Contracts;
using FiapSub.Sales.Domain.Catalog;

namespace FiapSub.Sales.Application.UseCases.Listings;

/// <summary>
/// Shared body of the two public listings: same query shape, same ordering, different status.
/// Reads the local projection only, so listing traffic never reaches the catalogue service.
/// </summary>
internal static class VehicleListingReader
{
    internal static async Task<Result<PagedList<VehicleListingResponse>>> ReadAsync(
        IVehicleSnapshotRepository snapshots,
        VehicleAvailability status,
        VehicleListingQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var total = await snapshots.CountByStatusAsync(status, cancellationToken);
        var page = await snapshots.ListByStatusOrderedByPriceAsync(
            status,
            (request.Page - 1) * request.PageSize,
            request.PageSize,
            cancellationToken);

        var items = page.Select(VehicleListingResponse.From).ToList();

        return Result.Success(
            new PagedList<VehicleListingResponse>(items, request.Page, request.PageSize, total));
    }
}
