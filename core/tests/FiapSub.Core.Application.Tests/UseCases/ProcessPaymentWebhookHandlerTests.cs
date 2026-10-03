using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.Tests.TestDoubles;
using FiapSub.Core.Application.UseCases.Payments.ProcessPaymentWebhook;
using FiapSub.Core.Domain.Payments;
using FiapSub.Core.Domain.ValueObjects;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.Tests.UseCases;

public class ProcessPaymentWebhookHandlerTests
{
    private static readonly PaymentCode Payment = PaymentCode.Create("PAY-0001");

    private readonly IPaymentNotificationRepository _notifications =
        Substitute.For<IPaymentNotificationRepository>();

    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedClock _clock = FixedClock.AtDefault();

    private ProcessPaymentWebhookHandler Handler() =>
        new(_notifications, _vehicles, _unitOfWork, _clock);

    [Fact]
    public async Task ApprovedPayment_SellsTheVehicleAndRecordsTheNotification()
    {
        var vehicle = VehicleFactory.Reserved(Payment);
        _notifications.GetByPaymentCodeAsync(Arg.Any<PaymentCode>(), Arg.Any<CancellationToken>())
            .Returns((PaymentNotification?)null);
        _vehicles.GetByReservationPaymentCodeAsync(Arg.Any<PaymentCode>(), Arg.Any<CancellationToken>())
            .Returns(vehicle);

        var result = await Handler().ExecuteAsync(
            new ProcessPaymentWebhookCommand(Payment.Value, PaymentOutcome.Approved),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AlreadyProcessed.ShouldBeFalse();
        result.Value.VehicleId.ShouldBe(vehicle.Id);
        result.Value.VehicleStatus.ShouldBe(nameof(VehicleStatus.Sold));
        vehicle.Status.ShouldBe(VehicleStatus.Sold);

        _notifications.Received(1).Add(Arg.Is<PaymentNotification>(n =>
            n.PaymentCode == Payment && n.Outcome == PaymentOutcome.Approved && n.VehicleId == vehicle.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelledPayment_ReleasesTheVehicle()
    {
        var vehicle = VehicleFactory.Reserved(Payment);
        _notifications.GetByPaymentCodeAsync(Arg.Any<PaymentCode>(), Arg.Any<CancellationToken>())
            .Returns((PaymentNotification?)null);
        _vehicles.GetByReservationPaymentCodeAsync(Arg.Any<PaymentCode>(), Arg.Any<CancellationToken>())
            .Returns(vehicle);

        var result = await Handler().ExecuteAsync(
            new ProcessPaymentWebhookCommand(Payment.Value, PaymentOutcome.Cancelled),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.VehicleStatus.ShouldBe(nameof(VehicleStatus.Available));
        vehicle.Status.ShouldBe(VehicleStatus.Available);
        vehicle.ReservationPaymentCode.ShouldBeNull();
    }

    [Fact]
    public async Task ReplayedNotification_ChangesNothing()
    {
        var vehicleId = Guid.CreateVersion7();
        var recorded = PaymentNotification.Record(Payment, PaymentOutcome.Approved, FixedClock.Default);
        recorded.MarkApplied(vehicleId, FixedClock.Default);
        _notifications.GetByPaymentCodeAsync(Arg.Any<PaymentCode>(), Arg.Any<CancellationToken>())
            .Returns(recorded);

        var result = await Handler().ExecuteAsync(
            new ProcessPaymentWebhookCommand(Payment.Value, PaymentOutcome.Approved),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AlreadyProcessed.ShouldBeTrue();
        result.Value.VehicleId.ShouldBe(vehicleId);

        _notifications.DidNotReceive().Add(Arg.Any<PaymentNotification>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _vehicles.DidNotReceive()
            .GetByReservationPaymentCodeAsync(Arg.Any<PaymentCode>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownPaymentCode_IsRecordedAsUnappliedAndReportedAsNotFound()
    {
        _notifications.GetByPaymentCodeAsync(Arg.Any<PaymentCode>(), Arg.Any<CancellationToken>())
            .Returns((PaymentNotification?)null);
        _vehicles.GetByReservationPaymentCodeAsync(Arg.Any<PaymentCode>(), Arg.Any<CancellationToken>())
            .Returns((Vehicle?)null);

        var result = await Handler().ExecuteAsync(
            new ProcessPaymentWebhookCommand(Payment.Value, PaymentOutcome.Approved),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCodes.PaymentVehicleNotFound);

        _notifications.Received(1).Add(Arg.Is<PaymentNotification>(n =>
            n.FailureReason != null && n.VehicleId == null && n.IsProcessed));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BlankPaymentCode_IsRejectedByTheValueObject()
    {
        await Should.ThrowAsync<Domain.Exceptions.DomainValidationException>(() => Handler().ExecuteAsync(
            new ProcessPaymentWebhookCommand("   ", PaymentOutcome.Approved),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task NullCommand_IsRejected()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            () => Handler().ExecuteAsync(null!, TestContext.Current.CancellationToken));
    }
}
