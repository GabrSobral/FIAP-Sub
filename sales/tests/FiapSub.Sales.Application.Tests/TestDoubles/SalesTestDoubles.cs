using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Domain.Catalog;
using FiapSub.Sales.Domain.Sales;
using FiapSub.Sales.Domain.ValueObjects;

namespace FiapSub.Sales.Application.Tests.TestDoubles;

internal sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
{
    internal static readonly DateTimeOffset Default = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);

    public DateTimeOffset UtcNow { get; set; } = now;

    internal static FixedClock AtDefault() => new(Default);
}

internal sealed class StubPaymentCodeGenerator(string value = "PAY-TEST-0001") : IPaymentCodeGenerator
{
    internal int Calls { get; private set; }

    public PaymentCode Next()
    {
        Calls++;
        return PaymentCode.Create(value);
    }
}

internal static class SalesFactory
{
    internal const string ValidCpf = "529.982.247-25";
    internal const string OtherValidCpf = "111.444.777-35";

    internal static VehicleSnapshot Snapshot(
        Guid? vehicleId = null,
        VehicleAvailability status = VehicleAvailability.Available,
        decimal price = 78_900.00m,
        long version = 1) =>
        VehicleSnapshot.Replicate(
            vehicleId ?? Guid.CreateVersion7(),
            "Fiat",
            "Argo Drive",
            2023,
            "Vermelho",
            price,
            "BRL",
            status,
            version,
            FixedClock.Default,
            FixedClock.Default);

    internal static Sale PendingSale(Guid vehicleId, string paymentCode = "PAY-TEST-0001")
    {
        var sale = Sale.Open(
            vehicleId,
            ValidCpf,
            78_900.00m,
            "BRL",
            PaymentCode.Create(paymentCode),
            requestedSaleDate: null,
            FixedClock.Default);

        sale.ClearDomainEvents();

        return sale;
    }
}
