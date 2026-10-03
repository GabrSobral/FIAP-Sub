using FiapSub.Sales.Domain.ValueObjects;

namespace FiapSub.Sales.Application.Abstractions;

/// <summary>
/// Mints the code that ties a sale to its payment. The sales service owns it because the sale is
/// opened here; the payment provider later quotes it back on the webhook.
/// </summary>
public interface IPaymentCodeGenerator
{
    PaymentCode Next();
}
