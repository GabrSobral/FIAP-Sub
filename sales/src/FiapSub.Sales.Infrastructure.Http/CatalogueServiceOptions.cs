namespace FiapSub.Sales.Infrastructure.Http;

/// <summary>Bound from the <c>CatalogueService</c> configuration section.</summary>
public sealed class CatalogueServiceOptions
{
    public const string SectionName = "CatalogueService";

    /// <summary>Header carrying the shared secret that guards the <c>/api/internal</c> routes.</summary>
    public const string InternalTokenHeader = "X-Internal-Token";

    public string BaseUrl { get; set; } = "http://core-api:8080";

    /// <summary>Shared secret both services agree on. Supplied by configuration or a k8s secret.</summary>
    public string InternalToken { get; set; } = string.Empty;

    /// <summary>Budget for a single attempt.</summary>
    public int AttemptTimeoutSeconds { get; set; } = 5;

    public int RetryCount { get; set; } = 3;
}
