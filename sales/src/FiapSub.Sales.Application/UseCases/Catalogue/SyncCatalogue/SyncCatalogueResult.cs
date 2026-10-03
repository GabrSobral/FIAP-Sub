namespace FiapSub.Sales.Application.UseCases.Catalogue.SyncCatalogue;

/// <param name="Pulled">Vehicles the catalogue returned.</param>
/// <param name="Applied">Of those, how many were new or newer than the local replica.</param>
public sealed record SyncCatalogueResult(int Pulled, int Applied);
