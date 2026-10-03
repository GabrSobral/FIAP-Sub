using System.Diagnostics.CodeAnalysis;
using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Domain.Payments;
using FiapSub.Core.Domain.Vehicles;
using FiapSub.Core.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FiapSub.Core.Infrastructure.Database;

[ExcludeFromCodeCoverage(Justification = "Composition root wiring; exercised implicitly by the integration tests.")]
public static class DependencyInjection
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        // Options are resolved from the provider rather than read here, so configuration added
        // after this call — as the integration tests do — is still honoured.
        services.AddDbContext<CoreDbContext>((provider, builder) =>
        {
            var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            builder.UseNpgsql(options.ConnectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(options.MaxRetryCount);
                npgsql.CommandTimeout(options.CommandTimeoutSeconds);
                npgsql.MigrationsHistoryTable("__ef_migrations_history");
            });

            if (options.EnableDetailedLogging)
            {
                builder.EnableDetailedErrors();
                builder.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IPaymentNotificationRepository, PaymentNotificationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
