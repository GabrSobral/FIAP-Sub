using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.Contracts;
using FiapSub.Core.Domain.ValueObjects;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.UseCases.Vehicles.ReleaseVehicle;

public sealed class ReleaseVehicleHandler(
    IVehicleRepository vehicles,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IUseCase<ReleaseVehicleCommand, VehicleResponse>
{
    public async Task<Result<VehicleResponse>> ExecuteAsync(
        ReleaseVehicleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var vehicle = await vehicles.GetByIdAsync(request.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Result.Failure<VehicleResponse>(
                Error.NotFound(ErrorCodes.VehicleNotFound, $"No vehicle with id {request.VehicleId}."));
        }

        vehicle.ReleaseReservation(PaymentCode.Create(request.PaymentCode), clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(VehicleResponse.From(vehicle));
    }
}
