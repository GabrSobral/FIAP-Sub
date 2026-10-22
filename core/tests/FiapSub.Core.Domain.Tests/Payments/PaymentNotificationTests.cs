using FiapSub.Core.Domain.Exceptions;
using FiapSub.Core.Domain.Payments;
using FiapSub.Core.Domain.ValueObjects;

namespace FiapSub.Core.Domain.Tests.Payments;

public class PaymentNotificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly PaymentCode Payment = PaymentCode.Create("PAY-0001");

    [Fact]
    public void Record_StartsUnprocessed()
    {
        var notification = PaymentNotification.Record(Payment, PaymentOutcome.Approved, Now);

        notification.Id.ShouldNotBe(Guid.Empty);
        notification.PaymentCode.ShouldBe(Payment);
        notification.Outcome.ShouldBe(PaymentOutcome.Approved);
        notification.ReceivedAt.ShouldBe(Now);
        notification.ProcessedAt.ShouldBeNull();
        notification.VehicleId.ShouldBeNull();
        notification.FailureReason.ShouldBeNull();
        notification.IsProcessed.ShouldBeFalse();
        notification.IsApplied.ShouldBeFalse();
    }

    [Fact]
    public void Record_RejectsNullPaymentCode()
    {
        Should.Throw<ArgumentNullException>(() => PaymentNotification.Record(null!, PaymentOutcome.Approved, Now));
    }

    [Fact]
    public void MarkApplied_LinksTheVehicleAndClearsAnyFailure()
    {
        var notification = PaymentNotification.Record(Payment, PaymentOutcome.Cancelled, Now);
        var vehicleId = Guid.CreateVersion7();
        notification.MarkUnapplied("transient glitch", Now);

        notification.MarkApplied(vehicleId, Now.AddSeconds(5));

        notification.VehicleId.ShouldBe(vehicleId);
        notification.ProcessedAt.ShouldBe(Now.AddSeconds(5));
        notification.FailureReason.ShouldBeNull();
        notification.IsProcessed.ShouldBeTrue();
        notification.IsApplied.ShouldBeTrue();
    }

    [Fact]
    public void MarkUnapplied_RecordsWhyAndStillCountsAsProcessed()
    {
        var notification = PaymentNotification.Record(Payment, PaymentOutcome.Approved, Now);

        notification.MarkUnapplied("  no vehicle holds this payment code  ", Now);

        notification.FailureReason.ShouldBe("no vehicle holds this payment code");
        notification.ProcessedAt.ShouldBe(Now);
        notification.IsProcessed.ShouldBeTrue();
        notification.IsApplied.ShouldBeFalse();
        notification.VehicleId.ShouldBeNull();
    }

    [Fact]
    public void Retry_OnAnUnappliedNotification_TakesTheLatestOutcome()
    {
        var notification = PaymentNotification.Record(Payment, PaymentOutcome.Approved, Now);
        notification.MarkUnapplied("no vehicle holds this payment code", Now);

        notification.Retry(PaymentOutcome.Cancelled);

        notification.Outcome.ShouldBe(PaymentOutcome.Cancelled);
        notification.ReceivedAt.ShouldBe(Now);
    }

    [Fact]
    public void Retry_OnAnAppliedNotification_IsRefused()
    {
        var notification = PaymentNotification.Record(Payment, PaymentOutcome.Approved, Now);
        notification.MarkApplied(Guid.CreateVersion7(), Now);

        Should.Throw<DomainStateException>(() => notification.Retry(PaymentOutcome.Cancelled));
        notification.Outcome.ShouldBe(PaymentOutcome.Approved);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkUnapplied_RejectsBlankReason(string? reason)
    {
        var notification = PaymentNotification.Record(Payment, PaymentOutcome.Approved, Now);

        Should.Throw<DomainValidationException>(() => notification.MarkUnapplied(reason!, Now));
    }

    [Fact]
    public void MarkUnapplied_RejectsOverlyLongReason()
    {
        var notification = PaymentNotification.Record(Payment, PaymentOutcome.Approved, Now);

        Should.Throw<DomainValidationException>(
            () => notification.MarkUnapplied(new string('x', PaymentNotification.MaxFailureReasonLength + 1), Now));
    }
}
