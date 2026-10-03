using FiapSub.Core.Domain.ValueObjects;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.Tests.TestDoubles;

internal static class VehicleFactory
{
    internal static Vehicle Available(decimal price = 78_900.00m) =>
        Vehicle.Register("Fiat", "Argo Drive", 2023, "Vermelho", price, FixedClock.Default);

    internal static Vehicle Reserved(PaymentCode paymentCode)
    {
        var vehicle = Available();
        vehicle.Reserve(paymentCode, FixedClock.Default);
        vehicle.ClearDomainEvents();
        return vehicle;
    }
}
