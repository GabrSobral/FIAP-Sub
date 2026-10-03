using FiapSub.Core.Application.EventHandlers;
using FiapSub.Core.Application.Gateways;
using FiapSub.Core.Application.Tests.TestDoubles;
using FiapSub.Core.Domain.Vehicles;
using FiapSub.Core.Domain.Vehicles.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapSub.Core.Application.Tests.Events;

public class CoreEventHandlerTests
{
    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly ISalesServiceGateway _salesService = Substitute.For<ISalesServiceGateway>();

    private SyncVehicleProjectionHandler ProjectionHandler() =>
        new(_vehicles, _salesService, NullLogger<SyncVehicleProjectionHandler>.Instance);

    [Fact]
    public void ProjectionHandler_ClaimsEveryProjectionEvent()
    {
        var handler = ProjectionHandler();
        var now = FixedClock.Default;
        var id = Guid.CreateVersion7();

        handler.CanHandle(new VehicleRegistered(id, now)).ShouldBeTrue();
        handler.CanHandle(new VehicleDetailsUpdated(id, now)).ShouldBeTrue();
        handler.CanHandle(new VehicleReserved(id, "PAY-1", now)).ShouldBeTrue();
        handler.CanHandle(new VehicleSold(id, "PAY-1", now)).ShouldBeTrue();
        handler.CanHandle(new VehicleReservationReleased(id, "PAY-1", now)).ShouldBeTrue();
    }

    [Fact]
    public async Task ProjectionHandler_PublishesTheCurrentVehicleState()
    {
        var vehicle = VehicleFactory.Available(55_000m);
        _vehicles.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        await ProjectionHandler().HandleAsync(
            new VehicleRegistered(vehicle.Id, FixedClock.Default),
            TestContext.Current.CancellationToken);

        await _salesService.Received(1).PublishVehicleAsync(
            Arg.Is<VehicleProjectionMessage>(m =>
                m.VehicleId == vehicle.Id
                && m.Price == 55_000m
                && m.Status == nameof(VehicleStatus.Available)
                && m.Version == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProjectionHandler_SkipsAVehicleThatNoLongerExists()
    {
        _vehicles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        await ProjectionHandler().HandleAsync(
            new VehicleRegistered(Guid.CreateVersion7(), FixedClock.Default),
            TestContext.Current.CancellationToken);

        await _salesService.DidNotReceive()
            .PublishVehicleAsync(Arg.Any<VehicleProjectionMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void PaymentResultHandler_ClaimsOnlyTheTerminalPaymentEvents()
    {
        var handler = new NotifySalesOfPaymentResultHandler(_salesService);
        var now = FixedClock.Default;
        var id = Guid.CreateVersion7();

        handler.CanHandle(new VehicleSold(id, "PAY-1", now)).ShouldBeTrue();
        handler.CanHandle(new VehicleReservationReleased(id, "PAY-1", now)).ShouldBeTrue();
        handler.CanHandle(new VehicleRegistered(id, now)).ShouldBeFalse();
        handler.CanHandle(new VehicleReserved(id, "PAY-1", now)).ShouldBeFalse();
    }

    [Fact]
    public async Task PaymentResultHandler_ReportsASoldVehicleAsApproved()
    {
        var id = Guid.CreateVersion7();

        await new NotifySalesOfPaymentResultHandler(_salesService).HandleAsync(
            new VehicleSold(id, "PAY-1", FixedClock.Default),
            TestContext.Current.CancellationToken);

        await _salesService.Received(1).PublishPaymentResultAsync(
            Arg.Is<PaymentResultMessage>(m =>
                m.PaymentCode == "PAY-1" && m.Status == PaymentResultMessage.Approved && m.VehicleId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PaymentResultHandler_ReportsAReleasedVehicleAsCancelled()
    {
        var id = Guid.CreateVersion7();

        await new NotifySalesOfPaymentResultHandler(_salesService).HandleAsync(
            new VehicleReservationReleased(id, "PAY-1", FixedClock.Default),
            TestContext.Current.CancellationToken);

        await _salesService.Received(1).PublishPaymentResultAsync(
            Arg.Is<PaymentResultMessage>(m => m.Status == PaymentResultMessage.Cancelled),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PaymentResultHandler_IgnoresAnUnrelatedEvent()
    {
        await new NotifySalesOfPaymentResultHandler(_salesService).HandleAsync(
            new VehicleRegistered(Guid.CreateVersion7(), FixedClock.Default),
            TestContext.Current.CancellationToken);

        await _salesService.DidNotReceive()
            .PublishPaymentResultAsync(Arg.Any<PaymentResultMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ProjectionMessage_RejectsANullVehicle()
    {
        Should.Throw<ArgumentNullException>(() => VehicleProjectionMessage.From(null!));
    }
}
