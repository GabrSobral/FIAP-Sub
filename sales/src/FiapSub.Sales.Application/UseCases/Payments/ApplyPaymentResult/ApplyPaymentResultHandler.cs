using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Contracts;
using FiapSub.Sales.Domain.Catalog;
using FiapSub.Sales.Domain.Sales;
using FiapSub.Sales.Domain.ValueObjects;

namespace FiapSub.Sales.Application.UseCases.Payments.ApplyPaymentResult;

/// <summary>
/// Closes or cancels the sale the payment belongs to. Idempotent: the catalogue reports results
/// at least once, and the aggregate treats a repeat as a no-op.
/// </summary>
public sealed class ApplyPaymentResultHandler(
    ISaleRepository sales,
    IVehicleSnapshotRepository snapshots,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IUseCase<ApplyPaymentResultCommand, SaleResponse>
{
    public async Task<Result<SaleResponse>> ExecuteAsync(
        ApplyPaymentResultCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.TryParseApproved(out var approved))
        {
            return Result.Failure<SaleResponse>(Error.Validation(
                ErrorCodes.ValidationFailed,
                $"Unknown payment status '{request.Status}'. Use '{ApplyPaymentResultCommand.Approved}' or '{ApplyPaymentResultCommand.Cancelled}'."));
        }

        var paymentCode = PaymentCode.Create(request.PaymentCode);
        var sale = await sales.GetByPaymentCodeAsync(paymentCode, cancellationToken);

        if (sale is null)
        {
            return Result.Failure<SaleResponse>(Error.NotFound(
                ErrorCodes.SaleNotFound,
                $"No sale carries payment code '{paymentCode.Value}'."));
        }

        var now = clock.UtcNow;

        if (approved)
        {
            sale.MarkPaid(now);
        }
        else
        {
            sale.Cancel("The payment was cancelled by the provider.", now);
        }

        // Keep the listings in step immediately; the catalogue's push confirms the same thing.
        var snapshot = await snapshots.GetByIdAsync(sale.VehicleId, cancellationToken);
        snapshot?.ProjectLocalStatus(
            approved ? VehicleAvailability.Sold : VehicleAvailability.Available,
            now);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SaleResponse.From(sale));
    }
}
