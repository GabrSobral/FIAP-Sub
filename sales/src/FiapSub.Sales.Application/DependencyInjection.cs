using System.Diagnostics.CodeAnalysis;
using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Abstractions.Events;
using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Contracts;
using FiapSub.Sales.Application.EventHandlers;
using FiapSub.Sales.Application.UseCases.Catalogue.SyncCatalogue;
using FiapSub.Sales.Application.UseCases.Catalogue.UpsertVehicleSnapshot;
using FiapSub.Sales.Application.UseCases.Listings.ListSoldVehicles;
using FiapSub.Sales.Application.UseCases.Listings.ListVehiclesForSale;
using FiapSub.Sales.Application.UseCases.Payments.ApplyPaymentResult;
using FiapSub.Sales.Application.UseCases.Sales.GetSale;
using FiapSub.Sales.Application.UseCases.Sales.OpenSale;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FiapSub.Sales.Application;

[ExcludeFromCodeCoverage(Justification = "Composition root wiring; exercised implicitly by the integration tests.")]
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddUseCase<ListVehiclesForSaleQuery, PagedList<VehicleListingResponse>, ListVehiclesForSaleHandler>();
        services.AddUseCase<ListSoldVehiclesQuery, PagedList<VehicleListingResponse>, ListSoldVehiclesHandler>();
        services.AddUseCase<OpenSaleCommand, SaleResponse, OpenSaleHandler>();
        services.AddUseCase<GetSaleQuery, SaleResponse, GetSaleHandler>();
        services.AddUseCase<UpsertVehicleSnapshotCommand, VehicleSnapshotAck, UpsertVehicleSnapshotHandler>();
        services.AddUseCase<ApplyPaymentResultCommand, SaleResponse, ApplyPaymentResultHandler>();
        services.AddUseCase<SyncCatalogueCommand, SyncCatalogueResult, SyncCatalogueHandler>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IDomainEventHandler, SaleAuditLogHandler>();

        services.AddValidatorsFromAssemblyContaining<OpenSaleValidator>(ServiceLifetime.Scoped);

        return services;
    }

    /// <summary>
    /// Registers a handler behind <see cref="ValidatingUseCase{TRequest,TResponse}"/>, so every use
    /// case validates its own request and no caller can skip that step.
    /// </summary>
    private static IServiceCollection AddUseCase<TRequest, TResponse, THandler>(this IServiceCollection services)
        where THandler : class, IUseCase<TRequest, TResponse>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IUseCase<TRequest, TResponse>>(provider => new ValidatingUseCase<TRequest, TResponse>(
            provider.GetRequiredService<THandler>(),
            provider.GetServices<IValidator<TRequest>>()));

        return services;
    }
}
