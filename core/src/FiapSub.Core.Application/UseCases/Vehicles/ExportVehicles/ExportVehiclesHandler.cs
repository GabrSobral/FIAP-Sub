using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.Gateways;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.UseCases.Vehicles.ExportVehicles;

public sealed class ExportVehiclesHandler(IVehicleRepository vehicles)
    : IUseCase<ExportVehiclesQuery, PagedList<VehicleProjectionMessage>>
{
    public async Task<Result<PagedList<VehicleProjectionMessage>>> ExecuteAsync(
        ExportVehiclesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var total = await vehicles.CountAsync(status: null, cancellationToken);
        var page = await vehicles.ListAsync(
            status: null,
            (request.Page - 1) * request.PageSize,
            request.PageSize,
            cancellationToken);

        var items = page.Select(VehicleProjectionMessage.From).ToList();

        return Result.Success(
            new PagedList<VehicleProjectionMessage>(items, request.Page, request.PageSize, total));
    }
}
