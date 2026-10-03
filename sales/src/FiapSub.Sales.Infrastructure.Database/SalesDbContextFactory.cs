using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FiapSub.Sales.Infrastructure.Database;

/// <summary>
/// Used only by <c>dotnet ef</c>. Building the context here, instead of letting the tool boot the
/// web host, keeps migration commands independent from the application's startup wiring.
/// Set <c>SALES_DB_CONNECTION</c> to point the tool at a different database.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Design-time only; never runs as part of the application.")]
public sealed class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    private const string DefaultConnection =
        "Host=localhost;Port=5433;Database=fiapsub_sales;Username=fiapsub;Password=fiapsub";

    public SalesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SALES_DB_CONNECTION") ?? DefaultConnection;

        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .Options;

        return new SalesDbContext(options);
    }
}
