using FiapSub.Sales.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapSub.Sales.Infrastructure.Database.Configurations;

public sealed class VehicleSnapshotConfiguration : IEntityTypeConfiguration<VehicleSnapshot>
{
    public void Configure(EntityTypeBuilder<VehicleSnapshot> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("vehicle_snapshots");

        builder.HasKey(s => s.Id);

        // The key is the catalogue's vehicle id, so it is never generated here.
        builder.Property(s => s.Id).HasColumnName("vehicle_id").ValueGeneratedNever();

        builder.Property(s => s.Brand)
            .HasColumnName("brand")
            .HasMaxLength(VehicleSnapshot.MaxBrandLength)
            .IsRequired();

        builder.Property(s => s.Model)
            .HasColumnName("model")
            .HasMaxLength(VehicleSnapshot.MaxModelLength)
            .IsRequired();

        builder.Property(s => s.Year).HasColumnName("model_year").IsRequired();

        builder.Property(s => s.Color)
            .HasColumnName("color")
            .HasMaxLength(VehicleSnapshot.MaxColorLength)
            .IsRequired();

        builder.Property(s => s.Price).HasColumnName("price").HasPrecision(18, 2).IsRequired();
        builder.Property(s => s.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Version).HasColumnName("version").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(s => s.ReplicatedAt).HasColumnName("replicated_at").IsRequired();

        builder.Ignore(s => s.DomainEvents);
        builder.Ignore(s => s.IsForSale);

        // Covers both public listings: filter on status, read out already ordered by price.
        builder.HasIndex(s => new { s.Status, s.Price })
            .HasDatabaseName("ix_vehicle_snapshots_status_price");
    }
}
