using FiapSub.Core.Domain.Payments;
using FiapSub.Core.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace FiapSub.Core.Infrastructure.Database;

/// <summary>
/// The catalogue service's own database. It holds the vehicle write model and the payment
/// notification ledger, and nothing that belongs to the sales service.
/// </summary>
public sealed class CoreDbContext(DbContextOptions<CoreDbContext> options) : DbContext(options)
{
    /// <summary>One DbSet per aggregate root; nothing else is loaded on its own.</summary>
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<PaymentNotification> PaymentNotifications => Set<PaymentNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);
    }
}
