using System.Diagnostics.CodeAnalysis;
using FiapSub.Sales.Application.Gateways;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace FiapSub.Sales.Infrastructure.Http;

[ExcludeFromCodeCoverage(Justification = "Composition root wiring; exercised implicitly by the integration tests.")]
public static class DependencyInjection
{
    public static IServiceCollection AddServiceGateways(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<CatalogueServiceOptions>(configuration.GetSection(CatalogueServiceOptions.SectionName));

        services
            .AddHttpClient<IVehicleCatalogueGateway, VehicleCatalogueGateway>((provider, client) =>
            {
                var options = Read(provider);

                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + '/');
                client.DefaultRequestHeaders.Add(CatalogueServiceOptions.InternalTokenHeader, options.InternalToken);
            })
            .AddStandardResilienceHandler()
            .Configure((resilience, provider) =>
            {
                var options = Read(provider);
                var attemptTimeout = TimeSpan.FromSeconds(options.AttemptTimeoutSeconds);

                resilience.AttemptTimeout.Timeout = attemptTimeout;
                resilience.Retry.MaxRetryAttempts = options.RetryCount;
                resilience.Retry.Delay = TimeSpan.FromMilliseconds(200);
                resilience.Retry.UseJitter = true;
                resilience.TotalRequestTimeout.Timeout =
                    attemptTimeout * (options.RetryCount + 1) + TimeSpan.FromSeconds(5);
                resilience.CircuitBreaker.SamplingDuration = attemptTimeout * 2 + TimeSpan.FromSeconds(10);
            });

        return services;
    }

    private static CatalogueServiceOptions Read(IServiceProvider provider) =>
        provider.GetRequiredService<IOptions<CatalogueServiceOptions>>().Value;
}
