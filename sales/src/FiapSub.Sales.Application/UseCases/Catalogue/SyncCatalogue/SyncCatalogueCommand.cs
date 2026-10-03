namespace FiapSub.Sales.Application.UseCases.Catalogue.SyncCatalogue;

/// <summary>
/// Pulls the catalogue and replays it into the local projection. This is the reconciliation path
/// for anything a push lost, and the way a fresh deployment of this service fills its listings.
/// </summary>
public sealed record SyncCatalogueCommand(int PageSize = 100)
{
    public const int MaxPageSize = 500;

    /// <summary>Stops a runaway pull if the catalogue keeps returning full pages.</summary>
    public const int MaxPages = 200;
}
