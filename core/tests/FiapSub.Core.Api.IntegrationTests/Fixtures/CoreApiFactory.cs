using FiapSub.Core.Application.Gateways;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FiapSub.Core.Api.IntegrationTests.Fixtures;

/// <summary>
/// Boots the real API pipeline — controllers, middleware, EF Core, migrations — against a
/// throwaway Postgres container. Only the outbound call to the sales service is replaced, since
/// the other service is out of scope for this repository's tests.
/// </summary>
internal sealed class CoreApiFactory(string connectionString, ISalesServiceGateway salesService)
    : WebApplicationFactory<Program>
{
    internal const string InternalToken = "integration-test-token";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Database:ConnectionString"] = connectionString,
                ["Database:RunMigrationsOnStartup"] = "true",
                ["Database:EnableDetailedLogging"] = "true",
                ["InternalApi:Token"] = InternalToken,
                ["SalesService:BaseUrl"] = "http://sales-api.invalid",
                ["SalesService:InternalToken"] = InternalToken,
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISalesServiceGateway>();
            services.AddSingleton(salesService);
        });
    }
}
