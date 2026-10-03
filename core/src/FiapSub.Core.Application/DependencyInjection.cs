using System.Diagnostics.CodeAnalysis;
using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Abstractions.Events;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.Contracts;
using FiapSub.Core.Application.EventHandlers;
using FiapSub.Core.Application.Gateways;
using FiapSub.Core.Application.UseCases.Payments.ProcessPaymentWebhook;
using FiapSub.Core.Application.UseCases.Vehicles.ExportVehicles;
using FiapSub.Core.Application.UseCases.Vehicles.GetVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.ListVehicles;
using FiapSub.Core.Application.UseCases.Vehicles.RegisterVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.ReleaseVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.ReserveVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.UpdateVehicle;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FiapSub.Core.Application;

[ExcludeFromCodeCoverage(Justification = "Composition root wiring; exercised implicitly by the integration tests.")]
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddUseCase<RegisterVehicleCommand, VehicleResponse, RegisterVehicleHandler>();
        services.AddUseCase<UpdateVehicleCommand, VehicleResponse, UpdateVehicleHandler>();
        services.AddUseCase<GetVehicleQuery, VehicleResponse, GetVehicleHandler>();
        services.AddUseCase<ListVehiclesQuery, PagedList<VehicleResponse>, ListVehiclesHandler>();
        services.AddUseCase<ReserveVehicleCommand, VehicleResponse, ReserveVehicleHandler>();
        services.AddUseCase<ReleaseVehicleCommand, VehicleResponse, ReleaseVehicleHandler>();
        services.AddUseCase<ExportVehiclesQuery, PagedList<VehicleProjectionMessage>, ExportVehiclesHandler>();
        services.AddUseCase<ProcessPaymentWebhookCommand, PaymentWebhookResponse, ProcessPaymentWebhookHandler>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IDomainEventHandler, SyncVehicleProjectionHandler>();
        services.AddScoped<IDomainEventHandler, NotifySalesOfPaymentResultHandler>();

        services.AddValidatorsFromAssemblyContaining<RegisterVehicleValidator>(ServiceLifetime.Scoped);

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
