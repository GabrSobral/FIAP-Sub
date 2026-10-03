using FiapSub.Core.Domain.Payments;
using FiapSub.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FiapSub.Core.Infrastructure.Database.Repositories;

internal sealed class PaymentNotificationRepository(CoreDbContext context) : IPaymentNotificationRepository
{
    public Task<PaymentNotification?> GetByPaymentCodeAsync(
        PaymentCode paymentCode,
        CancellationToken cancellationToken) =>
        context.PaymentNotifications.FirstOrDefaultAsync(n => n.PaymentCode == paymentCode, cancellationToken);

    public void Add(PaymentNotification notification) => context.PaymentNotifications.Add(notification);
}
