using System.Net.Http.Headers;
using FiapSub.Sales.Api.Infrastructure;
using FiapSub.Sales.Application.Gateways;
using Testcontainers.PostgreSql;

namespace FiapSub.Sales.Api.IntegrationTests.Fixtures;

/// <summary>
/// Owns the Postgres container and the test host for the whole integration collection, so the
/// container starts once instead of once per test class.
/// </summary>
public sealed class SalesApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("fiapsub_sales")
        .WithUsername("fiapsub")
        .WithPassword("fiapsub")
        .Build();

    private SalesApiFactory? _factory;

    /// <summary>Stub for the outbound half of the service-to-service contract.</summary>
    public IVehicleCatalogueGateway Catalogue { get; } = Substitute.For<IVehicleCatalogueGateway>();

    /// <summary>Client whose requests carry no internal token, like a public caller.</summary>
    public HttpClient Public { get; private set; } = null!;

    /// <summary>Client that presents the shared internal token, like the catalogue service.</summary>
    public HttpClient Internal { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync(TestContext.Current.CancellationToken);

        _factory = new SalesApiFactory(_database.GetConnectionString(), Catalogue);

        Public = _factory.CreateClient();

        Internal = _factory.CreateClient();
        Internal.DefaultRequestHeaders.Add(InternalApiOptions.HeaderName, SalesApiFactory.InternalToken);
        Internal.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async ValueTask DisposeAsync()
    {
        Public?.Dispose();
        Internal?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _database.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class SalesApiCollection : ICollectionFixture<SalesApiFixture>
{
    public const string Name = "sales-api";
}
