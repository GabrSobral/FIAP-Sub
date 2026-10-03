using FiapSub.Core.Domain.Abstractions;
using FiapSub.Core.Domain.ValueObjects;

namespace FiapSub.Core.Domain.Payments;

/// <summary>
/// The ledger of webhook calls received from the payment provider. One row per payment code,
/// enforced by a unique index, which is what makes the webhook idempotent: a replay finds the
/// notification already recorded and changes nothing.
/// </summary>
public sealed class PaymentNotification : AggregateRoot<Guid>
{
    public const int MaxFailureReasonLength = 500;

    /// <summary>Required by EF Core when materialising from the database.</summary>
    private PaymentNotification()
    {
    }

    private PaymentNotification(Guid id, PaymentCode paymentCode, PaymentOutcome outcome, DateTimeOffset now)
        : base(id)
    {
        PaymentCode = paymentCode;
        Outcome = outcome;
        ReceivedAt = now;
    }

    public PaymentCode PaymentCode { get; private set; } = default!;

    public PaymentOutcome Outcome { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    /// <summary>Set once the notification has been applied to a vehicle.</summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    public Guid? VehicleId { get; private set; }

    /// <summary>Why the notification could not be applied, when that is the case.</summary>
    public string? FailureReason { get; private set; }

    public bool IsProcessed => ProcessedAt is not null;

    public static PaymentNotification Record(PaymentCode paymentCode, PaymentOutcome outcome, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(paymentCode);

        return new PaymentNotification(Guid.CreateVersion7(), paymentCode, outcome, now);
    }

    public void MarkApplied(Guid vehicleId, DateTimeOffset now)
    {
        VehicleId = vehicleId;
        ProcessedAt = now;
        FailureReason = null;
    }

    public void MarkUnapplied(string reason, DateTimeOffset now)
    {
        FailureReason = Guard.RequiredText(reason, nameof(FailureReason), MaxFailureReasonLength);
        ProcessedAt = now;
    }
}
