using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.Contracts;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.UseCases.Vehicles.GetVehicle;

public sealed class GetVehicleHandler(IVehicleRepository vehicles)
    : IUseCase<GetVehicleQuery, VehicleResponse>
{
    public async Task<Result<VehicleResponse>> ExecuteAsync(
        GetVehicleQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var vehicle = await vehicles.GetByIdAsync(request.VehicleId, cancellationToken);

        return vehicle is null
            ? Result.Failure<VehicleResponse>(
                Error.NotFound(ErrorCodes.VehicleNotFound, $"No vehicle with id {request.VehicleId}."))
            : Result.Success(VehicleResponse.From(vehicle));
    }
}
