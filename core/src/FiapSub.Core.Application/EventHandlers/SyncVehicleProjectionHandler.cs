using FiapSub.Core.Application.Abstractions.Events;
using FiapSub.Core.Application.Gateways;
using FiapSub.Core.Domain.Abstractions;
using FiapSub.Core.Domain.Vehicles;
using FiapSub.Core.Domain.Vehicles.Events;
using Microsoft.Extensions.Logging;

namespace FiapSub.Core.Application.EventHandlers;

/// <summary>
/// Whenever a vehicle changes, republish it to the sales service so its read model — and
/// therefore the two public listings — stays current.
/// </summary>
public sealed class SyncVehicleProjectionHandler(
    IVehicleRepository vehicles,
    ISalesServiceGateway salesService,
    ILogger<SyncVehicleProjectionHandler> logger) : IDomainEventHandler
{
    public bool CanHandle(IDomainEvent domainEvent) => domainEvent is IVehicleProjectionEvent;

    public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var vehicleId = ((IVehicleProjectionEvent)domainEvent).VehicleId;
        var vehicle = await vehicles.GetByIdAsync(vehicleId, cancellationToken);

        if (vehicle is null)
        {
            logger.LogWarning("Vehicle {VehicleId} vanished before its projection could be published.", vehicleId);
            return;
        }

        await salesService.PublishVehicleAsync(VehicleProjectionMessage.From(vehicle), cancellationToken);
    }
}
