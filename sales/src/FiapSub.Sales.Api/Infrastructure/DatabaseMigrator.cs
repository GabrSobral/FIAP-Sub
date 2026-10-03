using FiapSub.Sales.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FiapSub.Sales.Api.Infrastructure;

internal static class DatabaseMigrator
{
    /// <summary>
    /// Applies the committed EF Core migrations when <c>Database:RunMigrationsOnStartup</c> is on.
    /// Never uses EnsureCreated: the migration history is the schema's source of truth.
    /// </summary>
    internal static async Task MigrateAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        if (!options.RunMigrationsOnStartup)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        app.Logger.LogInformation("Applying pending EF Core migrations to the sales database.");
        await context.Database.MigrateAsync();
    }
}
