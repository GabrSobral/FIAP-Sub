using FiapSub.Sales.Domain.Sales;
using FiapSub.Sales.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapSub.Sales.Infrastructure.Database.Configurations;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("sales");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.VehicleId).HasColumnName("vehicle_id").IsRequired();

        builder.Property(s => s.BuyerCpf)
            .HasColumnName("buyer_cpf")
            .HasConversion(cpf => cpf.Value, stored => Cpf.Create(stored))
            .HasMaxLength(Cpf.Length)
            .IsFixedLength()
            .IsRequired();

        builder.ComplexProperty(s => s.Amount, amount =>
        {
            amount.Property(m => m.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            amount.Property(m => m.Currency).HasColumnName("amount_currency").HasMaxLength(3).IsRequired();
            amount.Ignore(m => m.IsGreaterThanZero);
        });

        builder.Property(s => s.PaymentCode)
            .HasColumnName("payment_code")
            .HasConversion(code => code.Value, stored => PaymentCode.Create(stored))
            .HasMaxLength(PaymentCode.MaxLength)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.RequestedSaleDate).HasColumnName("requested_sale_date");
        builder.Property(s => s.SaleDate).HasColumnName("sale_date");

        builder.Property(s => s.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(Sale.MaxCancellationReasonLength);

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.Property(s => s.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.Ignore(s => s.DomainEvents);
        builder.Ignore(s => s.IsPending);

        // The payment code is how the catalogue reports a result back to us.
        builder.HasIndex(s => s.PaymentCode)
            .IsUnique()
            .HasDatabaseName("ux_sales_payment_code");

        // Two buyers must not have an open purchase for the same vehicle. The application checks
        // this too; the partial unique index is what makes the rule hold under a race, and it is
        // also the index that serves the pending-sale lookup.
        builder.HasIndex(s => s.VehicleId)
            .IsUnique()
            .HasFilter($"status = '{nameof(SaleStatus.PendingPayment)}'")
            .HasDatabaseName("ux_sales_pending_vehicle");
    }
}
