using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Tests.TestDoubles;
using FiapSub.Sales.Application.UseCases.Payments.ApplyPaymentResult;
using FiapSub.Sales.Domain.Catalog;
using FiapSub.Sales.Domain.Sales;
using FiapSub.Sales.Domain.ValueObjects;

namespace FiapSub.Sales.Application.Tests.UseCases;

public class ApplyPaymentResultHandlerTests
{
    private readonly ISaleRepository _sales = Substitute.For<ISaleRepository>();
    private readonly IVehicleSnapshotRepository _snapshots = Substitute.For<IVehicleSnapshotRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedClock _clock = FixedClock.AtDefault();

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    private ApplyPaymentResultHandler Handler() => new(_sales, _snapshots, _unitOfWork, _clock);

    private (Sale Sale, VehicleSnapshot Snapshot) GivenPendingSale()
    {
        var snapshot = SalesFactory.Snapshot(status: VehicleAvailability.Reserved);
        var sale = SalesFactory.PendingSale(snapshot.Id);

        _sales.GetByPaymentCodeAsync(Arg.Any<PaymentCode>(), Arg.Any<CancellationToken>()).Returns(sale);
        _snapshots.GetByIdAsync(snapshot.Id, Arg.Any<CancellationToken>()).Returns(snapshot);

        return (sale, snapshot);
    }

    [Fact]
    public async Task ApprovedPayment_ClosesTheSaleAndStampsTheSaleDate()
    {
        var (sale, snapshot) = GivenPendingSale();
        _clock.UtcNow = FixedClock.Default.AddMinutes(9);

        var result = await Handler().ExecuteAsync(
            new ApplyPaymentResultCommand(sale.PaymentCode.Value, "approved"),
            Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(nameof(SaleStatus.Paid));
        result.Value.SaleDate.ShouldBe(_clock.UtcNow);
        sale.Status.ShouldBe(SaleStatus.Paid);
        snapshot.Status.ShouldBe(VehicleAvailability.Sold);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelledPayment_CancelsTheSaleAndRelistsTheVehicle()
    {
        var (sale, snapshot) = GivenPendingSale();

        var result = await Handler().ExecuteAsync(
            new ApplyPaymentResultCommand(sale.PaymentCode.Value, "cancelled"),
            Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(nameof(SaleStatus.Cancelled));
        result.Value.CancellationReason.ShouldNotBeNull().ShouldContain("cancelled by the provider");
        result.Value.SaleDate.ShouldBeNull();
        snapshot.Status.ShouldBe(VehicleAvailability.Available);
    }

    [Fact]
    public async Task ReplayedResult_IsANoOpOnTheSale()
    {
        var (sale, _) = GivenPendingSale();
        await Handler().ExecuteAsync(new ApplyPaymentResultCommand(sale.PaymentCode.Value, "approved"), Ct);
        var versionAfterFirst = sale.Version;

        var result = await Handler().ExecuteAsync(
            new ApplyPaymentResultCommand(sale.PaymentCode.Value, "approved"),
            Ct);

        result.IsSuccess.ShouldBeTrue();
        sale.Version.ShouldBe(versionAfterFirst);
    }

    [Fact]
    public async Task AMissingProjectionDoesNotStopTheSaleFromClosing()
    {
        var snapshot = SalesFactory.Snapshot();
        var sale = SalesFactory.PendingSale(snapshot.Id);
        _sales.GetByPaymentCodeAsync(Arg.Any<PaymentCode>(), Arg.Any<CancellationToken>()).Returns(sale);
        _snapshots.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VehicleSnapshot?)null);

        var result = await Handler().ExecuteAsync(
            new ApplyPaymentResultCommand(sale.PaymentCode.Value, "approved"),
            Ct);

        result.IsSuccess.ShouldBeTrue();
        sale.Status.ShouldBe(SaleStatus.Paid);
    }

    [Fact]
    public async Task AnUnknownPaymentCode_IsReportedAsNotFound()
    {
        _sales.GetByPaymentCodeAsync(Arg.Any<PaymentCode>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var result = await Handler().ExecuteAsync(new ApplyPaymentResultCommand("PAY-UNKNOWN", "approved"), Ct);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCodes.SaleNotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("half-paid")]
    [InlineData("")]
    [InlineData(null)]
    public async Task AnUnknownStatus_IsAValidationFailure(string? status)
    {
        var result = await Handler().ExecuteAsync(new ApplyPaymentResultCommand("PAY-1", status), Ct);

        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.Message.ShouldContain("Unknown payment status");
    }

    [Theory]
    [InlineData("APPROVED", true)]
    [InlineData("paid", true)]
    [InlineData("CANCELED", false)]
    [InlineData("rejected", false)]
    public void StatusParsingAcceptsTheProviderWordingVariants(string status, bool expected)
    {
        new ApplyPaymentResultCommand("PAY-1", status).TryParseApproved(out var approved).ShouldBeTrue();
        approved.ShouldBe(expected);
    }

    [Fact]
    public async Task RejectsANullCommand()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => Handler().ExecuteAsync(null!, Ct));
    }
}
