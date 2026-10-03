using FiapSub.Core.Domain.Abstractions;
using FiapSub.Core.Domain.Exceptions;
using FiapSub.Core.Domain.ValueObjects;
using FiapSub.Core.Domain.Vehicles.Events;

namespace FiapSub.Core.Domain.Vehicles;

/// <summary>
/// A vehicle in the resale catalogue, and the aggregate that owns its selling lifecycle.
/// This service is the single source of truth for a vehicle's status; the sales service only
/// keeps a replica of it.
/// </summary>
public sealed class Vehicle : AggregateRoot<Guid>
{
    public const int MaxBrandLength = 60;
    public const int MaxModelLength = 80;
    public const int MaxColorLength = 30;

    /// <summary>Required by EF Core when materialising from the database.</summary>
    private Vehicle()
    {
    }

    private Vehicle(
        Guid id,
        string brand,
        string model,
        ModelYear year,
        string color,
        Money price,
        DateTimeOffset now)
        : base(id)
    {
        Brand = brand;
        Model = model;
        Year = year;
        Color = color;
        Price = price;
        Status = VehicleStatus.Available;
        CreatedAt = now;
        UpdatedAt = now;
        Version = 1;

        Raise(new VehicleRegistered(id, now));
    }

    public string Brand { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public ModelYear Year { get; private set; }

    public string Color { get; private set; } = string.Empty;

    public Money Price { get; private set; } = Money.Zero;

    public VehicleStatus Status { get; private set; }

    /// <summary>Set while the vehicle is reserved or sold; it links the vehicle to its payment.</summary>
    public PaymentCode? ReservationPaymentCode { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Bumped on every state change. Doubles as the optimistic-concurrency token and as the
    /// ordering key the sales service uses to discard out-of-order projection updates.
    /// </summary>
    public long Version { get; private set; }

    public static Vehicle Register(
        string? brand,
        string? model,
        int year,
        string? color,
        decimal price,
        DateTimeOffset now) =>
        new(
            Guid.CreateVersion7(),
            Guard.RequiredText(brand, nameof(Brand), MaxBrandLength),
            Guard.RequiredText(model, nameof(Model), MaxModelLength),
            ModelYear.Create(year, now.Year + 1),
            Guard.RequiredText(color, nameof(Color), MaxColorLength),
            SellingPrice(price),
            now);

    public void UpdateDetails(
        string? brand,
        string? model,
        int year,
        string? color,
        decimal price,
        DateTimeOffset now)
    {
        if (Status != VehicleStatus.Available)
        {
            throw new DomainStateException(
                $"Only an available vehicle can be edited; this one is {Status}. "
                + "Cancel the purchase before changing the vehicle's data.");
        }

        Brand = Guard.RequiredText(brand, nameof(Brand), MaxBrandLength);
        Model = Guard.RequiredText(model, nameof(Model), MaxModelLength);
        Year = ModelYear.Create(year, now.Year + 1);
        Color = Guard.RequiredText(color, nameof(Color), MaxColorLength);
        Price = SellingPrice(price);

        Touch(now);
        Raise(new VehicleDetailsUpdated(Id, now));
    }

    /// <summary>
    /// Holds the vehicle for a purchase. Replaying the same payment code is a no-op, because the
    /// sales service may retry this call.
    /// </summary>
    public void Reserve(PaymentCode paymentCode, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(paymentCode);

        if (Status == VehicleStatus.Reserved && ReservationPaymentCode == paymentCode)
        {
            return;
        }

        if (Status != VehicleStatus.Available)
        {
            throw new DomainStateException($"Only an available vehicle can be reserved; this one is {Status}.");
        }

        ReservationPaymentCode = paymentCode;
        Status = VehicleStatus.Reserved;

        Touch(now);
        Raise(new VehicleReserved(Id, paymentCode.Value, now));
    }

    /// <summary>Payment approved: the vehicle is sold. Replays are no-ops.</summary>
    public void ConfirmSale(PaymentCode paymentCode, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(paymentCode);

        if (Status == VehicleStatus.Sold && ReservationPaymentCode == paymentCode)
        {
            return;
        }

        EnsureReservedWith(paymentCode);

        Status = VehicleStatus.Sold;

        Touch(now);
        Raise(new VehicleSold(Id, paymentCode.Value, now));
    }

    /// <summary>Payment cancelled: the vehicle goes back on the shelf. Replays are no-ops.</summary>
    public void ReleaseReservation(PaymentCode paymentCode, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(paymentCode);

        if (Status == VehicleStatus.Available && ReservationPaymentCode is null)
        {
            return;
        }

        EnsureReservedWith(paymentCode);

        Status = VehicleStatus.Available;
        ReservationPaymentCode = null;

        Touch(now);
        Raise(new VehicleReservationReleased(Id, paymentCode.Value, now));
    }

    private void EnsureReservedWith(PaymentCode paymentCode)
    {
        if (Status != VehicleStatus.Reserved)
        {
            throw new DomainStateException($"The vehicle is not reserved; its status is {Status}.");
        }

        if (ReservationPaymentCode != paymentCode)
        {
            throw new DomainStateException("The payment code does not match this vehicle's reservation.");
        }
    }

    private static Money SellingPrice(decimal price)
    {
        var money = Money.Create(price);

        if (!money.IsGreaterThanZero)
        {
            throw new DomainValidationException("Vehicle price must be greater than zero.");
        }

        return money;
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
        Version++;
    }
}
