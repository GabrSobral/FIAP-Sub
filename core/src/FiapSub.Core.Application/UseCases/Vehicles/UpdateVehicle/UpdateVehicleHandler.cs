using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.Contracts;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.UseCases.Vehicles.UpdateVehicle;

public sealed class UpdateVehicleHandler(
    IVehicleRepository vehicles,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IUseCase<UpdateVehicleCommand, VehicleResponse>
{
    public async Task<Result<VehicleResponse>> ExecuteAsync(
        UpdateVehicleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var vehicle = await vehicles.GetByIdAsync(request.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Result.Failure<VehicleResponse>(
                Error.NotFound(ErrorCodes.VehicleNotFound, $"No vehicle with id {request.VehicleId}."));
        }

        vehicle.UpdateDetails(
            request.Brand,
            request.Model,
            request.Year,
            request.Color,
            request.Price,
            clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(VehicleResponse.From(vehicle));
    }
}
