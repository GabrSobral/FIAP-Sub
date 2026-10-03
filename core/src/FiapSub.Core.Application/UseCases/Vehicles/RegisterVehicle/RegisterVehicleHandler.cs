using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.Contracts;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.UseCases.Vehicles.RegisterVehicle;

public sealed class RegisterVehicleHandler(
    IVehicleRepository vehicles,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IUseCase<RegisterVehicleCommand, VehicleResponse>
{
    public async Task<Result<VehicleResponse>> ExecuteAsync(
        RegisterVehicleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var vehicle = Vehicle.Register(
            request.Brand,
            request.Model,
            request.Year,
            request.Color,
            request.Price,
            clock.UtcNow);

        vehicles.Add(vehicle);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(VehicleResponse.From(vehicle));
    }
}
