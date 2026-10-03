namespace FiapSub.Core.Api.Infrastructure;

/// <summary>Bound from the <c>InternalApi</c> configuration section.</summary>
public sealed class InternalApiOptions
{
    public const string SectionName = "InternalApi";

    public const string HeaderName = "X-Internal-Token";

    /// <summary>
    /// Shared secret the sales service must present on <c>/api/internal/*</c>. Left empty on
    /// purpose in the committed configuration: those routes refuse every call until it is set.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Route prefix the token guards.</summary>
    public string PathPrefix { get; set; } = "/api/internal";
}
