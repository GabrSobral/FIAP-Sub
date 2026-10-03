using FiapSub.Sales.Domain.Abstractions;
using FiapSub.Sales.Domain.Exceptions;
using FiapSub.Sales.Domain.Sales.Events;
using FiapSub.Sales.Domain.ValueObjects;

namespace FiapSub.Sales.Domain.Sales;

/// <summary>
/// A purchase of one vehicle by one buyer. This aggregate is the reason the sales service owns a
/// database: the catalogue knows a vehicle is sold, but who bought it, for how much and on what
/// date lives here.
/// </summary>
public sealed class Sale : AggregateRoot<Guid>
{
    public const int MaxCancellationReasonLength = 500;

    /// <summary>Required by EF Core when materialising from the database.</summary>
    private Sale()
    {
    }

    private Sale(
        Guid id,
        Guid vehicleId,
        Cpf buyerCpf,
        Money amount,
        PaymentCode paymentCode,
        DateTimeOffset? requestedSaleDate,
        DateTimeOffset now)
        : base(id)
    {
        VehicleId = vehicleId;
        BuyerCpf = buyerCpf;
        Amount = amount;
        PaymentCode = paymentCode;
        RequestedSaleDate = requestedSaleDate;
        Status = SaleStatus.PendingPayment;
        CreatedAt = now;
        UpdatedAt = now;
        Version = 1;

        Raise(new SaleOpened(id, vehicleId, paymentCode.Value, now));
    }

    public Guid VehicleId { get; private set; }

    public Cpf BuyerCpf { get; private set; } = default!;

    /// <summary>The vehicle's price at the moment the purchase was opened.</summary>
    public Money Amount { get; private set; } = Money.Zero;

    /// <summary>Generated here and handed to the payment provider; the webhook comes back with it.</summary>
    public PaymentCode PaymentCode { get; private set; } = default!;

    public SaleStatus Status { get; private set; }

    /// <summary>
    /// A sale date the caller asked for. When absent the payment's confirmation instant is used.
    /// </summary>
    public DateTimeOffset? RequestedSaleDate { get; private set; }

    /// <summary>Set once the payment is approved. This is the date of the sale.</summary>
    public DateTimeOffset? SaleDate { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Bumped on every change; mapped as the optimistic-concurrency token.</summary>
    public long Version { get; private set; }

    public bool IsPending => Status == SaleStatus.PendingPayment;

    public static Sale Open(
        Guid vehicleId,
        string? buyerCpf,
        decimal amount,
        string? currency,
        PaymentCode paymentCode,
        DateTimeOffset? requestedSaleDate,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(paymentCode);

        if (vehicleId == Guid.Empty)
        {
            throw new DomainValidationException("Vehicle id is required.");
        }

        var money = Money.Create(amount, currency ?? Money.DefaultCurrency);

        if (!money.IsGreaterThanZero)
        {
            throw new DomainValidationException("A sale amount must be greater than zero.");
        }

        if (requestedSaleDate > now)
        {
            throw new DomainValidationException("The sale date cannot be in the future.");
        }

        return new Sale(
            Guid.CreateVersion7(),
            vehicleId,
            Cpf.Create(buyerCpf),
            money,
            paymentCode,
            requestedSaleDate,
            now);
    }

    /// <summary>
    /// Closes the sale once the payment is approved. Replays are no-ops: the catalogue reports
    /// payment results at least once.
    /// </summary>
    public void MarkPaid(DateTimeOffset confirmedAt)
    {
        if (Status == SaleStatus.Paid)
        {
            return;
        }

        EnsurePending("completed");

        SaleDate = RequestedSaleDate ?? confirmedAt;
        Status = SaleStatus.Paid;

        Touch(confirmedAt);
        Raise(new SalePaid(Id, VehicleId, SaleDate.Value, confirmedAt));
    }

    /// <summary>
    /// Closes the sale as not happening — the payment was refused, or the vehicle could not be
    /// held. Replays are no-ops.
    /// </summary>
    public void Cancel(string? reason, DateTimeOffset now)
    {
        if (Status == SaleStatus.Cancelled)
        {
            return;
        }

        EnsurePending("cancelled");

        CancellationReason = Guard.RequiredText(reason, nameof(CancellationReason), MaxCancellationReasonLength);
        Status = SaleStatus.Cancelled;

        Touch(now);
        Raise(new SaleCancelled(Id, VehicleId, CancellationReason, now));
    }

    private void EnsurePending(string action)
    {
        if (Status != SaleStatus.PendingPayment)
        {
            throw new DomainStateException($"A sale that is {Status} can no longer be {action}.");
        }
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
        Version++;
    }
}
