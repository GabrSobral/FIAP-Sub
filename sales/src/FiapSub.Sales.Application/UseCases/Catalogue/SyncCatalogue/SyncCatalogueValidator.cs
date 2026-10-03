using FluentValidation;

namespace FiapSub.Sales.Application.UseCases.Catalogue.SyncCatalogue;

public sealed class SyncCatalogueValidator : AbstractValidator<SyncCatalogueCommand>
{
    public SyncCatalogueValidator()
    {
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(SyncCatalogueCommand.MaxPageSize);
    }
}
