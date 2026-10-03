using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Contracts;
using FiapSub.Sales.Application.Gateways;
using FiapSub.Sales.Domain.Catalog;
using FiapSub.Sales.Domain.Sales;
using Microsoft.Extensions.Logging;

namespace FiapSub.Sales.Application.UseCases.Sales.OpenSale;

/// <summary>
/// Opens a purchase, which spans both services and therefore runs as a small saga:
/// <list type="number">
/// <item>the sale is written here first, so nothing is ever reserved without a record of why;</item>
/// <item>the catalogue is asked to hold the vehicle;</item>
/// <item>if that fails, the sale is cancelled, and an ambiguous failure also triggers a
/// best-effort release so the catalogue is not left holding a vehicle nobody is buying.</item>
/// </list>
/// </summary>
public sealed class OpenSaleHandler(
    ISaleRepository sales,
    IVehicleSnapshotRepository snapshots,
    IVehicleCatalogueGateway catalogue,
    IPaymentCodeGenerator paymentCodes,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ILogger<OpenSaleHandler> logger) : IUseCase<OpenSaleCommand, SaleResponse>
{
    public async Task<Result<SaleResponse>> ExecuteAsync(
        OpenSaleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await snapshots.GetByIdAsync(request.VehicleId, cancellationToken);

        if (snapshot is null)
        {
            return Result.Failure<SaleResponse>(Error.NotFound(
                ErrorCodes.VehicleNotInCatalogue,
                $"Vehicle {request.VehicleId} is not in this service's catalogue projection."));
        }

        if (!snapshot.IsForSale)
        {
            return Result.Failure<SaleResponse>(Error.Conflict(
                ErrorCodes.VehicleNotAvailable,
                $"Vehicle {request.VehicleId} is {snapshot.Status} and cannot be bought."));
        }

        var pending = await sales.GetPendingByVehicleAsync(request.VehicleId, cancellationToken);

        if (pending is not null)
        {
            return Result.Failure<SaleResponse>(Error.Conflict(
                ErrorCodes.SaleAlreadyPending,
                $"Sale {pending.Id} is already awaiting payment for vehicle {request.VehicleId}."));
        }

        var now = clock.UtcNow;
        var paymentCode = paymentCodes.Next();

        var sale = Sale.Open(
            request.VehicleId,
            request.BuyerCpf,
            snapshot.Price,
            snapshot.Currency,
            paymentCode,
            request.SaleDate,
            now);

        sales.Add(sale);

        // Take the vehicle off the public listing right away; the catalogue's push confirms it.
        snapshot.ProjectLocalStatus(VehicleAvailability.Reserved, now);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var reservation = await catalogue.ReserveAsync(sale.VehicleId, paymentCode.Value, cancellationToken);

        return reservation.Succeeded
            ? Result.Success(SaleResponse.From(sale))
            : await CompensateAsync(sale, snapshot, reservation, cancellationToken);
    }

    private async Task<Result<SaleResponse>> CompensateAsync(
        Sale sale,
        VehicleSnapshot snapshot,
        CatalogueReservation reservation,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Cancelling sale {SaleId}: the catalogue refused to hold vehicle {VehicleId} ({Outcome}: {Detail}).",
            sale.Id,
            sale.VehicleId,
            reservation.Outcome,
            reservation.Detail);

        if (reservation.Outcome == CatalogueReservationOutcome.CatalogueUnavailable)
        {
            // The reserve call may have landed before the connection failed, so drop the hold.
            await catalogue.TryReleaseAsync(sale.VehicleId, sale.PaymentCode.Value, cancellationToken);
        }

        var now = clock.UtcNow;
        sale.Cancel($"The catalogue could not hold the vehicle ({reservation.Outcome}).", now);

        // Put it back on the listing; the catalogue's own state still decides the final answer.
        snapshot.ProjectLocalStatus(
            reservation.Outcome == CatalogueReservationOutcome.VehicleUnavailable
                ? VehicleAvailability.Reserved
                : VehicleAvailability.Available,
            now);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Failure<SaleResponse>(reservation.Outcome switch
        {
            CatalogueReservationOutcome.VehicleNotFound => Error.NotFound(
                ErrorCodes.VehicleNotInCatalogue,
                $"The catalogue has no vehicle {sale.VehicleId}."),

            CatalogueReservationOutcome.VehicleUnavailable => Error.Conflict(
                ErrorCodes.VehicleNotAvailable,
                $"Vehicle {sale.VehicleId} is no longer available."),

            _ => Error.Conflict(
                ErrorCodes.CatalogueUnavailable,
                "The catalogue service could not be reached; the purchase was not completed."),
        });
    }
}
