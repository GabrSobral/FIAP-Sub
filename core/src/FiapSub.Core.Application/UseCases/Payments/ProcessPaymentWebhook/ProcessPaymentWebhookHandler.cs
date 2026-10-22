using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Domain.Payments;
using FiapSub.Core.Domain.ValueObjects;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.UseCases.Payments.ProcessPaymentWebhook;

/// <summary>
/// Applies a payment notification to the vehicle it belongs to. The notification ledger makes
/// this idempotent: the provider may call us more than once for the same payment, and only the
/// first call that finds the vehicle moves it. A call that finds none is recorded but left open,
/// so a retry that arrives after the reservation is committed still applies.
/// </summary>
public sealed class ProcessPaymentWebhookHandler(
    IPaymentNotificationRepository notifications,
    IVehicleRepository vehicles,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IUseCase<ProcessPaymentWebhookCommand, PaymentWebhookResponse>
{
    public async Task<Result<PaymentWebhookResponse>> ExecuteAsync(
        ProcessPaymentWebhookCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paymentCode = PaymentCode.Create(request.PaymentCode);
        var notification = await notifications.GetByPaymentCodeAsync(paymentCode, cancellationToken);

        if (notification is { IsApplied: true })
        {
            return Result.Success(new PaymentWebhookResponse(
                paymentCode.Value,
                notification.Outcome.ToString(),
                AlreadyProcessed: true,
                notification.VehicleId,
                VehicleStatus: null));
        }

        var now = clock.UtcNow;

        if (notification is null)
        {
            notification = PaymentNotification.Record(paymentCode, request.Outcome, now);
            notifications.Add(notification);
        }
        else
        {
            notification.Retry(request.Outcome);
        }

        var vehicle = await vehicles.GetByReservationPaymentCodeAsync(paymentCode, cancellationToken);

        if (vehicle is null)
        {
            notification.MarkUnapplied("No vehicle is reserved with this payment code.", now);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<PaymentWebhookResponse>(Error.NotFound(
                ErrorCodes.PaymentVehicleNotFound,
                $"No vehicle is reserved with payment code '{paymentCode.Value}'."));
        }

        if (request.Outcome == PaymentOutcome.Approved)
        {
            vehicle.ConfirmSale(paymentCode, now);
        }
        else
        {
            vehicle.ReleaseReservation(paymentCode, now);
        }

        notification.MarkApplied(vehicle.Id, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PaymentWebhookResponse(
            paymentCode.Value,
            request.Outcome.ToString(),
            AlreadyProcessed: false,
            vehicle.Id,
            vehicle.Status.ToString()));
    }
}
