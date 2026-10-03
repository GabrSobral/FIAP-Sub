using FiapSub.Sales.Domain.Abstractions;
using FiapSub.Sales.Domain.Exceptions;

namespace FiapSub.Sales.Domain.Catalog;

/// <summary>
/// The local replica of a vehicle from the catalogue service. It exists so the two public
/// listings can be served entirely from this service's own database, which is what keeps a
/// traffic spike here from reaching the catalogue.
/// </summary>
/// <remarks>
/// Its <see cref="Id"/> is the catalogue's vehicle id — same identity, different context.
/// </remarks>
public sealed class VehicleSnapshot : AggregateRoot<Guid>
{
    public const int MaxBrandLength = 60;
    public const int MaxModelLength = 80;
    public const int MaxColorLength = 30;

    /// <summary>Required by EF Core when materialising from the database.</summary>
    private VehicleSnapshot()
    {
    }

    private VehicleSnapshot(Guid vehicleId, DateTimeOffset replicatedAt) : base(vehicleId) =>
        ReplicatedAt = replicatedAt;

    public string Brand { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public int Year { get; private set; }

    public string Color { get; private set; } = string.Empty;

    /// <summary>
    /// Flat decimal rather than a <c>Money</c> value object: this is a read model whose whole
    /// purpose is being ordered by price in SQL, and a scalar column can carry the index.
    /// </summary>
    public decimal Price { get; private set; }

    public string Currency { get; private set; } = "BRL";

    public VehicleAvailability Status { get; private set; }

    /// <summary>The catalogue's version of the vehicle this replica reflects.</summary>
    public long Version { get; private set; }

    /// <summary>When the vehicle changed in the catalogue.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>When this service last wrote the replica.</summary>
    public DateTimeOffset ReplicatedAt { get; private set; }

    public bool IsForSale => Status == VehicleAvailability.Available;

    public static VehicleSnapshot Replicate(
        Guid vehicleId,
        string? brand,
        string? model,
        int year,
        string? color,
        decimal price,
        string? currency,
        VehicleAvailability status,
        long version,
        DateTimeOffset updatedAt,
        DateTimeOffset now)
    {
        if (vehicleId == Guid.Empty)
        {
            throw new DomainValidationException("Vehicle id is required.");
        }

        var snapshot = new VehicleSnapshot(vehicleId, now);
        snapshot.Overwrite(brand, model, year, color, price, currency, status, version, updatedAt, now);

        return snapshot;
    }

    /// <summary>
    /// Applies an incoming update from the catalogue. Pushes are at-least-once and can arrive out
    /// of order, so an update that is not strictly newer is ignored rather than applied.
    /// </summary>
    /// <returns><c>true</c> when the replica changed.</returns>
    public bool TryApplyUpdate(
        string? brand,
        string? model,
        int year,
        string? color,
        decimal price,
        string? currency,
        VehicleAvailability status,
        long version,
        DateTimeOffset updatedAt,
        DateTimeOffset now)
    {
        if (version <= Version)
        {
            return false;
        }

        Overwrite(brand, model, year, color, price, currency, status, version, updatedAt, now);

        return true;
    }

    /// <summary>
    /// Changes the replica's status locally, without pretending to be a new catalogue version.
    /// The sales service does this so the public listings react the instant a purchase is opened
    /// or settled, instead of lagging behind the catalogue's push. Because
    /// <see cref="Version"/> is untouched, the next authoritative push always wins.
    /// </summary>
    public void ProjectLocalStatus(VehicleAvailability status, DateTimeOffset now)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException($"Unknown vehicle status '{status}'.");
        }

        Status = status;
        ReplicatedAt = now;
    }

    private void Overwrite(
        string? brand,
        string? model,
        int year,
        string? color,
        decimal price,
        string? currency,
        VehicleAvailability status,
        long version,
        DateTimeOffset updatedAt,
        DateTimeOffset now)
    {
        if (version <= 0)
        {
            throw new DomainValidationException($"Vehicle version must be positive (got {version}).");
        }

        if (price <= 0m)
        {
            throw new DomainValidationException("Vehicle price must be greater than zero.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException($"Unknown vehicle status '{status}'.");
        }

        Brand = Guard.RequiredText(brand, nameof(Brand), MaxBrandLength);
        Model = Guard.RequiredText(model, nameof(Model), MaxModelLength);
        Color = Guard.RequiredText(color, nameof(Color), MaxColorLength);
        Currency = Guard.RequiredText(currency, nameof(Currency), 3).ToUpperInvariant();
        Year = year;
        Price = price;
        Status = status;
        Version = version;
        UpdatedAt = updatedAt;
        ReplicatedAt = now;
    }
}
