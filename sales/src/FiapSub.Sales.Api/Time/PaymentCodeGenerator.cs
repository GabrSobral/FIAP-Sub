using System.Diagnostics.CodeAnalysis;
using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Domain.ValueObjects;

namespace FiapSub.Sales.Api.Time;

/// <summary>
/// Mints payment codes as <c>PAY-{uuidv7}</c>: unique, collision-free across instances, and
/// sortable by creation time, which makes support lookups straightforward.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Thin wrapper over Guid generation; there is nothing to assert.")]
internal sealed class PaymentCodeGenerator : IPaymentCodeGenerator
{
    public PaymentCode Next() => PaymentCode.Create($"PAY-{Guid.CreateVersion7():N}");
}
