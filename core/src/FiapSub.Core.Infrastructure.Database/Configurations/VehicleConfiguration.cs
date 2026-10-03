using FiapSub.Core.Domain.ValueObjects;
using FiapSub.Core.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapSub.Core.Infrastructure.Database.Configurations;

/// <summary>
/// Mapping lives here rather than on the entity so the domain project stays free of any
/// persistence dependency.
/// </summary>
public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("vehicles");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(v => v.Brand)
            .HasColumnName("brand")
            .HasMaxLength(Vehicle.MaxBrandLength)
            .IsRequired();

        builder.Property(v => v.Model)
            .HasColumnName("model")
            .HasMaxLength(Vehicle.MaxModelLength)
            .IsRequired();

        builder.Property(v => v.Color)
            .HasColumnName("color")
            .HasMaxLength(Vehicle.MaxColorLength)
            .IsRequired();

        builder.Property(v => v.Year)
            .HasColumnName("model_year")
            .HasConversion(year => year.Value, stored => ModelYear.Restore(stored))
            .IsRequired();

        // Money is a complex type: it lives in the vehicles row, not in a table of its own.
        builder.ComplexProperty(v => v.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("price").HasPrecision(18, 2).IsRequired();
            price.Property(m => m.Currency).HasColumnName("price_currency").HasMaxLength(3).IsRequired();
            price.Ignore(m => m.IsGreaterThanZero);
        });

        builder.Property(v => v.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.ReservationPaymentCode)
            .HasColumnName("reservation_payment_code")
            .HasConversion(code => code!.Value, stored => PaymentCode.Create(stored))
            .HasMaxLength(PaymentCode.MaxLength);

        builder.Property(v => v.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Domain-owned version: bumped by the aggregate on every change, which makes it both the
        // optimistic-concurrency token and the ordering key the sales projection trusts.
        builder.Property(v => v.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.Ignore(v => v.DomainEvents);

        builder.HasIndex(v => v.Status).HasDatabaseName("ix_vehicles_status");

        // A payment code identifies exactly one vehicle, which is what lets the webhook find it.
        builder.HasIndex(v => v.ReservationPaymentCode)
            .IsUnique()
            .HasDatabaseName("ux_vehicles_reservation_payment_code");
    }
}
