using FiapSub.Core.Domain.Exceptions;
using FiapSub.Core.Domain.ValueObjects;
using FiapSub.Core.Domain.Vehicles;
using FiapSub.Core.Domain.Vehicles.Events;

namespace FiapSub.Core.Domain.Tests.Vehicles;

public class VehicleTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly PaymentCode Payment = PaymentCode.Create("PAY-0001");

    private static Vehicle Available() =>
        Vehicle.Register("Fiat", "Argo Drive", 2023, "Vermelho", 78_900.00m, Now);

    private static Vehicle Reserved()
    {
        var vehicle = Available();
        vehicle.Reserve(Payment, Now);
        vehicle.ClearDomainEvents();
        return vehicle;
    }

    [Fact]
    public void Register_StartsAvailableAndRaisesRegistered()
    {
        var vehicle = Available();

        vehicle.Id.ShouldNotBe(Guid.Empty);
        vehicle.Brand.ShouldBe("Fiat");
        vehicle.Model.ShouldBe("Argo Drive");
        vehicle.Year.Value.ShouldBe(2023);
        vehicle.Color.ShouldBe("Vermelho");
        vehicle.Price.ShouldBe(Money.Create(78_900.00m));
        vehicle.Status.ShouldBe(VehicleStatus.Available);
        vehicle.ReservationPaymentCode.ShouldBeNull();
        vehicle.CreatedAt.ShouldBe(Now);
        vehicle.UpdatedAt.ShouldBe(Now);
        vehicle.Version.ShouldBe(1);

        var registered = vehicle.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<VehicleRegistered>();
        registered.VehicleId.ShouldBe(vehicle.Id);
        registered.OccurredOn.ShouldBe(Now);
    }

    [Fact]
    public void Register_TrimsTextFields()
    {
        var vehicle = Vehicle.Register("  Fiat ", " Argo ", 2023, " Vermelho ", 1m, Now);

        vehicle.Brand.ShouldBe("Fiat");
        vehicle.Model.ShouldBe("Argo");
        vehicle.Color.ShouldBe("Vermelho");
    }

    [Fact]
    public void Register_AcceptsNextYearsModel()
    {
        Vehicle.Register("Fiat", "Argo", Now.Year + 1, "Vermelho", 1m, Now)
            .Year.Value.ShouldBe(Now.Year + 1);
    }

    [Fact]
    public void Register_RejectsModelYearBeyondNextYear()
    {
        Should.Throw<DomainValidationException>(
            () => Vehicle.Register("Fiat", "Argo", Now.Year + 2, "Vermelho", 1m, Now));
    }

    [Theory]
    [InlineData(null, "Argo", "Vermelho", "Brand is required")]
    [InlineData("  ", "Argo", "Vermelho", "Brand is required")]
    [InlineData("Fiat", null, "Vermelho", "Model is required")]
    [InlineData("Fiat", "Argo", "  ", "Color is required")]
    public void Register_RejectsBlankText(string? brand, string? model, string? color, string expected)
    {
        Should.Throw<DomainValidationException>(() => Vehicle.Register(brand, model, 2023, color, 1m, Now))
            .Message.ShouldContain(expected);
    }

    [Fact]
    public void Register_RejectsOverlyLongBrand()
    {
        Should.Throw<DomainValidationException>(
                () => Vehicle.Register(new string('x', Vehicle.MaxBrandLength + 1), "Argo", 2023, "Vermelho", 1m, Now))
            .Message.ShouldContain("at most 60 characters");
    }

    [Fact]
    public void Register_RejectsOverlyLongModel()
    {
        Should.Throw<DomainValidationException>(
                () => Vehicle.Register("Fiat", new string('x', Vehicle.MaxModelLength + 1), 2023, "Vermelho", 1m, Now))
            .Message.ShouldContain("at most 80 characters");
    }

    [Fact]
    public void Register_RejectsOverlyLongColor()
    {
        Should.Throw<DomainValidationException>(
                () => Vehicle.Register("Fiat", "Argo", 2023, new string('x', Vehicle.MaxColorLength + 1), 1m, Now))
            .Message.ShouldContain("at most 30 characters");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Register_RejectsNonPositivePrice(decimal price)
    {
        Should.Throw<DomainValidationException>(() => Vehicle.Register("Fiat", "Argo", 2023, "Vermelho", price, Now));
    }

    [Fact]
    public void UpdateDetails_ReplacesDataBumpsVersionAndRaisesEvent()
    {
        var vehicle = Available();
        vehicle.ClearDomainEvents();
        var later = Now.AddDays(1);

        vehicle.UpdateDetails("Volkswagen", "Polo Track", 2024, "Prata", 92_000.00m, later);

        vehicle.Brand.ShouldBe("Volkswagen");
        vehicle.Model.ShouldBe("Polo Track");
        vehicle.Year.Value.ShouldBe(2024);
        vehicle.Color.ShouldBe("Prata");
        vehicle.Price.ShouldBe(Money.Create(92_000.00m));
        vehicle.UpdatedAt.ShouldBe(later);
        vehicle.CreatedAt.ShouldBe(Now);
        vehicle.Version.ShouldBe(2);

        vehicle.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<VehicleDetailsUpdated>()
            .VehicleId.ShouldBe(vehicle.Id);
    }

    [Fact]
    public void UpdateDetails_RejectedWhileReserved()
    {
        Should.Throw<DomainStateException>(
                () => Reserved().UpdateDetails("Fiat", "Argo", 2023, "Azul", 1m, Now))
            .Message.ShouldContain("Only an available vehicle can be edited");
    }

    [Fact]
    public void UpdateDetails_RejectedOnceSold()
    {
        var vehicle = Reserved();
        vehicle.ConfirmSale(Payment, Now);

        Should.Throw<DomainStateException>(() => vehicle.UpdateDetails("Fiat", "Argo", 2023, "Azul", 1m, Now));
    }

    [Fact]
    public void Reserve_MovesToReservedAndStoresThePaymentCode()
    {
        var vehicle = Available();
        vehicle.ClearDomainEvents();
        var later = Now.AddMinutes(5);

        vehicle.Reserve(Payment, later);

        vehicle.Status.ShouldBe(VehicleStatus.Reserved);
        vehicle.ReservationPaymentCode.ShouldBe(Payment);
        vehicle.UpdatedAt.ShouldBe(later);
        vehicle.Version.ShouldBe(2);

        var reserved = vehicle.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<VehicleReserved>();
        reserved.PaymentCode.ShouldBe(Payment.Value);
    }

    [Fact]
    public void Reserve_IsIdempotentForTheSamePaymentCode()
    {
        var vehicle = Reserved();

        vehicle.Reserve(Payment, Now.AddHours(1));

        vehicle.Version.ShouldBe(2);
        vehicle.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Reserve_RejectsADifferentPaymentCodeWhileReserved()
    {
        Should.Throw<DomainStateException>(() => Reserved().Reserve(PaymentCode.Create("PAY-9999"), Now))
            .Message.ShouldContain("Only an available vehicle can be reserved");
    }

    [Fact]
    public void Reserve_RejectsNullPaymentCode()
    {
        Should.Throw<ArgumentNullException>(() => Available().Reserve(null!, Now));
    }

    [Fact]
    public void ConfirmSale_MovesToSoldAndKeepsThePaymentCode()
    {
        var vehicle = Reserved();
        var later = Now.AddMinutes(10);

        vehicle.ConfirmSale(Payment, later);

        vehicle.Status.ShouldBe(VehicleStatus.Sold);
        vehicle.ReservationPaymentCode.ShouldBe(Payment);
        vehicle.UpdatedAt.ShouldBe(later);
        vehicle.Version.ShouldBe(3);

        vehicle.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<VehicleSold>()
            .PaymentCode.ShouldBe(Payment.Value);
    }

    [Fact]
    public void ConfirmSale_IsIdempotentForTheSamePaymentCode()
    {
        var vehicle = Reserved();
        vehicle.ConfirmSale(Payment, Now);
        vehicle.ClearDomainEvents();

        vehicle.ConfirmSale(Payment, Now.AddHours(1));

        vehicle.Version.ShouldBe(3);
        vehicle.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ConfirmSale_RejectedWhenNotReserved()
    {
        Should.Throw<DomainStateException>(() => Available().ConfirmSale(Payment, Now))
            .Message.ShouldContain("not reserved");
    }

    [Fact]
    public void ConfirmSale_RejectsAForeignPaymentCode()
    {
        Should.Throw<DomainStateException>(() => Reserved().ConfirmSale(PaymentCode.Create("PAY-OTHER"), Now))
            .Message.ShouldContain("does not match");
    }

    [Fact]
    public void ConfirmSale_RejectsNullPaymentCode()
    {
        Should.Throw<ArgumentNullException>(() => Reserved().ConfirmSale(null!, Now));
    }

    [Fact]
    public void ReleaseReservation_PutsTheVehicleBackOnTheShelf()
    {
        var vehicle = Reserved();
        var later = Now.AddMinutes(10);

        vehicle.ReleaseReservation(Payment, later);

        vehicle.Status.ShouldBe(VehicleStatus.Available);
        vehicle.ReservationPaymentCode.ShouldBeNull();
        vehicle.Version.ShouldBe(3);

        vehicle.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<VehicleReservationReleased>()
            .PaymentCode.ShouldBe(Payment.Value);
    }

    [Fact]
    public void ReleaseReservation_IsIdempotentOnAnUnreservedVehicle()
    {
        var vehicle = Available();
        vehicle.ClearDomainEvents();

        vehicle.ReleaseReservation(Payment, Now);

        vehicle.Version.ShouldBe(1);
        vehicle.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ReleaseReservation_RejectedOnceSold()
    {
        var vehicle = Reserved();
        vehicle.ConfirmSale(Payment, Now);

        Should.Throw<DomainStateException>(() => vehicle.ReleaseReservation(Payment, Now))
            .Message.ShouldContain("not reserved");
    }

    [Fact]
    public void ReleaseReservation_RejectsAForeignPaymentCode()
    {
        Should.Throw<DomainStateException>(() => Reserved().ReleaseReservation(PaymentCode.Create("PAY-OTHER"), Now));
    }

    [Fact]
    public void ReleaseReservation_RejectsNullPaymentCode()
    {
        Should.Throw<ArgumentNullException>(() => Reserved().ReleaseReservation(null!, Now));
    }

    [Fact]
    public void FullLifecycle_ReleaseThenSellAgain()
    {
        var vehicle = Available();
        var first = PaymentCode.Create("PAY-A");
        var second = PaymentCode.Create("PAY-B");

        vehicle.Reserve(first, Now);
        vehicle.ReleaseReservation(first, Now.AddMinutes(1));
        vehicle.Reserve(second, Now.AddMinutes(2));
        vehicle.ConfirmSale(second, Now.AddMinutes(3));

        vehicle.Status.ShouldBe(VehicleStatus.Sold);
        vehicle.ReservationPaymentCode.ShouldBe(second);
        vehicle.Version.ShouldBe(5);
    }
}
