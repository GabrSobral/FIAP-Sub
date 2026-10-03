using System.Net.Http.Headers;
using FiapSub.Core.Api.Infrastructure;
using FiapSub.Core.Application.Gateways;
using Testcontainers.PostgreSql;

namespace FiapSub.Core.Api.IntegrationTests.Fixtures;

/// <summary>
/// Owns the Postgres container and the test host for the whole integration collection, so the
/// container starts once instead of once per test class.
/// </summary>
public sealed class CoreApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("fiapsub_core")
        .WithUsername("fiapsub")
        .WithPassword("fiapsub")
        .Build();

    private CoreApiFactory? _factory;

    /// <summary>Stub for the outbound half of the service-to-service contract.</summary>
    public ISalesServiceGateway SalesService { get; } = Substitute.For<ISalesServiceGateway>();

    /// <summary>Client whose requests carry no internal token, like a public caller.</summary>
    public HttpClient Public { get; private set; } = null!;

    /// <summary>Client that presents the shared internal token, like the sales service.</summary>
    public HttpClient Internal { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync(TestContext.Current.CancellationToken);

        _factory = new CoreApiFactory(_database.GetConnectionString(), SalesService);

        Public = _factory.CreateClient();

        Internal = _factory.CreateClient();
        Internal.DefaultRequestHeaders.Add(InternalApiOptions.HeaderName, CoreApiFactory.InternalToken);
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
public sealed class CoreApiCollection : ICollectionFixture<CoreApiFixture>
{
    public const string Name = "core-api";
}
