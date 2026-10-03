using FiapSub.Core.Domain.ValueObjects;

namespace FiapSub.Core.Domain.Payments;

public interface IPaymentNotificationRepository
{
    Task<PaymentNotification?> GetByPaymentCodeAsync(PaymentCode paymentCode, CancellationToken cancellationToken);

    void Add(PaymentNotification notification);
}
