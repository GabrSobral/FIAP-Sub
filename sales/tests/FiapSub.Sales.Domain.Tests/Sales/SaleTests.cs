using FiapSub.Sales.Domain.Exceptions;
using FiapSub.Sales.Domain.Sales;
using FiapSub.Sales.Domain.Sales.Events;
using FiapSub.Sales.Domain.ValueObjects;

namespace FiapSub.Sales.Domain.Tests.Sales;

public class SaleTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
    private const string ValidCpf = "529.982.247-25";

    private static Sale Open(DateTimeOffset? requestedSaleDate = null) => Sale.Open(
        Guid.CreateVersion7(),
        ValidCpf,
        78_900.00m,
        "BRL",
        PaymentCode.Create("PAY-0001"),
        requestedSaleDate,
        Now);

    [Fact]
    public void Open_StartsPendingAndRaisesSaleOpened()
    {
        var sale = Open();

        sale.Id.ShouldNotBe(Guid.Empty);
        sale.BuyerCpf.Value.ShouldBe("52998224725");
        sale.Amount.ShouldBe(Money.Create(78_900.00m));
        sale.PaymentCode.Value.ShouldBe("PAY-0001");
        sale.Status.ShouldBe(SaleStatus.PendingPayment);
        sale.IsPending.ShouldBeTrue();
        sale.SaleDate.ShouldBeNull();
        sale.CancellationReason.ShouldBeNull();
        sale.CreatedAt.ShouldBe(Now);
        sale.Version.ShouldBe(1);

        var opened = sale.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SaleOpened>();
        opened.SaleId.ShouldBe(sale.Id);
        opened.VehicleId.ShouldBe(sale.VehicleId);
        opened.PaymentCode.ShouldBe("PAY-0001");
    }

    [Fact]
    public void Open_RejectsAnEmptyVehicleId()
    {
        Should.Throw<DomainValidationException>(() => Sale.Open(
            Guid.Empty, ValidCpf, 1m, "BRL", PaymentCode.Create("PAY-1"), null, Now))
            .Message.ShouldContain("Vehicle id is required");
    }

    [Fact]
    public void Open_RejectsANullPaymentCode()
    {
        Should.Throw<ArgumentNullException>(() => Sale.Open(
            Guid.CreateVersion7(), ValidCpf, 1m, "BRL", null!, null, Now));
    }

    [Fact]
    public void Open_RejectsAnInvalidCpf()
    {
        Should.Throw<DomainValidationException>(() => Sale.Open(
            Guid.CreateVersion7(), "11111111111", 1m, "BRL", PaymentCode.Create("PAY-1"), null, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Open_RejectsANonPositiveAmount(decimal amount)
    {
        Should.Throw<DomainValidationException>(() => Sale.Open(
            Guid.CreateVersion7(), ValidCpf, amount, "BRL", PaymentCode.Create("PAY-1"), null, Now));
    }

    [Fact]
    public void Open_DefaultsTheCurrencyWhenNoneIsGiven()
    {
        Sale.Open(Guid.CreateVersion7(), ValidCpf, 1m, null, PaymentCode.Create("PAY-1"), null, Now)
            .Amount.Currency.ShouldBe("BRL");
    }

    [Fact]
    public void Open_RejectsAFutureSaleDate()
    {
        Should.Throw<DomainValidationException>(() => Open(Now.AddDays(1)))
            .Message.ShouldContain("cannot be in the future");
    }

    [Fact]
    public void MarkPaid_StampsTheConfirmationInstantAsTheSaleDate()
    {
        var sale = Open();
        sale.ClearDomainEvents();
        var confirmedAt = Now.AddMinutes(7);

        sale.MarkPaid(confirmedAt);

        sale.Status.ShouldBe(SaleStatus.Paid);
        sale.SaleDate.ShouldBe(confirmedAt);
        sale.UpdatedAt.ShouldBe(confirmedAt);
        sale.Version.ShouldBe(2);
        sale.IsPending.ShouldBeFalse();

        var paid = sale.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SalePaid>();
        paid.SaleDate.ShouldBe(confirmedAt);
    }

    [Fact]
    public void MarkPaid_HonoursARequestedSaleDate()
    {
        var requested = Now.AddDays(-2);
        var sale = Open(requested);

        sale.MarkPaid(Now.AddMinutes(7));

        sale.SaleDate.ShouldBe(requested);
    }

    [Fact]
    public void MarkPaid_IsIdempotent()
    {
        var sale = Open();
        sale.MarkPaid(Now);
        sale.ClearDomainEvents();

        sale.MarkPaid(Now.AddHours(1));

        sale.Version.ShouldBe(2);
        sale.SaleDate.ShouldBe(Now);
        sale.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void MarkPaid_IsRefusedOnACancelledSale()
    {
        var sale = Open();
        sale.Cancel("buyer gave up", Now);

        Should.Throw<DomainStateException>(() => sale.MarkPaid(Now))
            .Message.ShouldContain("can no longer be completed");
    }

    [Fact]
    public void Cancel_RecordsTheReason()
    {
        var sale = Open();
        sale.ClearDomainEvents();

        sale.Cancel("  payment refused  ", Now.AddMinutes(3));

        sale.Status.ShouldBe(SaleStatus.Cancelled);
        sale.CancellationReason.ShouldBe("payment refused");
        sale.SaleDate.ShouldBeNull();
        sale.Version.ShouldBe(2);

        sale.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<SaleCancelled>()
            .Reason.ShouldBe("payment refused");
    }

    [Fact]
    public void Cancel_IsIdempotent()
    {
        var sale = Open();
        sale.Cancel("payment refused", Now);
        sale.ClearDomainEvents();

        sale.Cancel("some other reason", Now.AddHours(1));

        sale.Version.ShouldBe(2);
        sale.CancellationReason.ShouldBe("payment refused");
        sale.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Cancel_IsRefusedOnAPaidSale()
    {
        var sale = Open();
        sale.MarkPaid(Now);

        Should.Throw<DomainStateException>(() => sale.Cancel("too late", Now))
            .Message.ShouldContain("can no longer be cancelled");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancel_RequiresAReason(string? reason)
    {
        Should.Throw<DomainValidationException>(() => Open().Cancel(reason, Now));
    }

    [Fact]
    public void Cancel_RejectsAnOverlyLongReason()
    {
        Should.Throw<DomainValidationException>(
            () => Open().Cancel(new string('x', Sale.MaxCancellationReasonLength + 1), Now));
    }
}
