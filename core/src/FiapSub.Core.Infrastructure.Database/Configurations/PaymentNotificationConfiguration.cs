using FiapSub.Core.Domain.Payments;
using FiapSub.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapSub.Core.Infrastructure.Database.Configurations;

public sealed class PaymentNotificationConfiguration : IEntityTypeConfiguration<PaymentNotification>
{
    public void Configure(EntityTypeBuilder<PaymentNotification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("payment_notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(n => n.PaymentCode)
            .HasColumnName("payment_code")
            .HasConversion(code => code.Value, stored => PaymentCode.Create(stored))
            .HasMaxLength(PaymentCode.MaxLength)
            .IsRequired();

        builder.Property(n => n.Outcome)
            .HasColumnName("outcome")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(n => n.ReceivedAt).HasColumnName("received_at").IsRequired();
        builder.Property(n => n.ProcessedAt).HasColumnName("processed_at");
        builder.Property(n => n.VehicleId).HasColumnName("vehicle_id");

        builder.Property(n => n.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(PaymentNotification.MaxFailureReasonLength);

        builder.Ignore(n => n.DomainEvents);
        builder.Ignore(n => n.IsProcessed);

        // This is what makes the webhook idempotent: a replayed notification cannot be inserted twice.
        builder.HasIndex(n => n.PaymentCode)
            .IsUnique()
            .HasDatabaseName("ux_payment_notifications_payment_code");
    }
}
