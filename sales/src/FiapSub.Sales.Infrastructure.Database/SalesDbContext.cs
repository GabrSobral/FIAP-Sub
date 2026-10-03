using FiapSub.Sales.Domain.Catalog;
using FiapSub.Sales.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace FiapSub.Sales.Infrastructure.Database;

/// <summary>
/// The sales service's own database, separate from the catalogue's. It holds the sales this
/// service owns plus a replica of the vehicles it needs to list, and nothing the catalogue owns.
/// </summary>
public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    /// <summary>One DbSet per aggregate root; nothing else is loaded on its own.</summary>
    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<VehicleSnapshot> VehicleSnapshots => Set<VehicleSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }
}
