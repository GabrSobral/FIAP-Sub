using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.Contracts;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.UseCases.Vehicles.ListVehicles;

public sealed class ListVehiclesHandler(IVehicleRepository vehicles)
    : IUseCase<ListVehiclesQuery, PagedList<VehicleResponse>>
{
    public async Task<Result<PagedList<VehicleResponse>>> ExecuteAsync(
        ListVehiclesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var total = await vehicles.CountAsync(request.Status, cancellationToken);
        var page = await vehicles.ListAsync(
            request.Status,
            (request.Page - 1) * request.PageSize,
            request.PageSize,
            cancellationToken);

        var items = page.Select(VehicleResponse.From).ToList();

        return Result.Success(new PagedList<VehicleResponse>(items, request.Page, request.PageSize, total));
    }
}
