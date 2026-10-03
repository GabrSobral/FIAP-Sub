using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.Contracts;
using FiapSub.Core.Domain.ValueObjects;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.UseCases.Vehicles.ReserveVehicle;

public sealed class ReserveVehicleHandler(
    IVehicleRepository vehicles,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IUseCase<ReserveVehicleCommand, VehicleResponse>
{
    public async Task<Result<VehicleResponse>> ExecuteAsync(
        ReserveVehicleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var vehicle = await vehicles.GetByIdAsync(request.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Result.Failure<VehicleResponse>(
                Error.NotFound(ErrorCodes.VehicleNotFound, $"No vehicle with id {request.VehicleId}."));
        }

        vehicle.Reserve(PaymentCode.Create(request.PaymentCode), clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(VehicleResponse.From(vehicle));
    }
}
