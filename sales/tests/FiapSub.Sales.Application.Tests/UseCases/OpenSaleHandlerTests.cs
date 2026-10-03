using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Gateways;
using FiapSub.Sales.Application.Tests.TestDoubles;
using FiapSub.Sales.Application.UseCases.Sales.OpenSale;
using FiapSub.Sales.Domain.Catalog;
using FiapSub.Sales.Domain.Sales;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapSub.Sales.Application.Tests.UseCases;

public class OpenSaleHandlerTests
{
    private readonly ISaleRepository _sales = Substitute.For<ISaleRepository>();
    private readonly IVehicleSnapshotRepository _snapshots = Substitute.For<IVehicleSnapshotRepository>();
    private readonly IVehicleCatalogueGateway _catalogue = Substitute.For<IVehicleCatalogueGateway>();
    private readonly StubPaymentCodeGenerator _paymentCodes = new();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedClock _clock = FixedClock.AtDefault();

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    private OpenSaleHandler Handler() => new(
        _sales,
        _snapshots,
        _catalogue,
        _paymentCodes,
        _unitOfWork,
        _clock,
        NullLogger<OpenSaleHandler>.Instance);

    private VehicleSnapshot GivenAvailableVehicle(decimal price = 78_900.00m)
    {
        var snapshot = SalesFactory.Snapshot(price: price);
        _snapshots.GetByIdAsync(snapshot.Id, Arg.Any<CancellationToken>()).Returns(snapshot);
        _sales.GetPendingByVehicleAsync(snapshot.Id, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        return snapshot;
    }

    [Fact]
    public async Task OpensThePurchaseAndHasTheCatalogueHoldTheVehicle()
    {
        var snapshot = GivenAvailableVehicle(55_000m);
        _catalogue.ReserveAsync(snapshot.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CatalogueReservation.Reserved());

        var result = await Handler().ExecuteAsync(
            new OpenSaleCommand(snapshot.Id, SalesFactory.ValidCpf, null),
            Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.VehicleId.ShouldBe(snapshot.Id);
        result.Value.Status.ShouldBe(nameof(SaleStatus.PendingPayment));
        result.Value.Amount.ShouldBe(55_000m);
        result.Value.PaymentCode.ShouldBe("PAY-TEST-0001");
        result.Value.BuyerCpf.ShouldBe("529.982.247-25");
        result.Value.SaleDate.ShouldBeNull();

        _sales.Received(1).Add(Arg.Is<Sale>(s => s.VehicleId == snapshot.Id));
        _paymentCodes.Calls.ShouldBe(1);

        // Taken off the public listing immediately, before the catalogue's own push arrives.
        snapshot.Status.ShouldBe(VehicleAvailability.Reserved);
        snapshot.Version.ShouldBe(1);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HonoursARequestedSaleDate()
    {
        var snapshot = GivenAvailableVehicle();
        _catalogue.ReserveAsync(snapshot.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CatalogueReservation.Reserved());
        var requested = FixedClock.Default.AddDays(-1);

        var result = await Handler().ExecuteAsync(
            new OpenSaleCommand(snapshot.Id, SalesFactory.ValidCpf, requested),
            Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.SaleDate.ShouldBeNull("the sale date is only stamped once the payment settles");
    }

    [Fact]
    public async Task ReportsAVehicleTheProjectionDoesNotKnow()
    {
        _snapshots.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VehicleSnapshot?)null);

        var result = await Handler().ExecuteAsync(
            new OpenSaleCommand(Guid.CreateVersion7(), SalesFactory.ValidCpf, null),
            Ct);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCodes.VehicleNotInCatalogue);
        await _catalogue.DidNotReceive().ReserveAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(VehicleAvailability.Reserved)]
    [InlineData(VehicleAvailability.Sold)]
    public async Task ReportsAVehicleThatIsNotForSale(VehicleAvailability status)
    {
        var snapshot = SalesFactory.Snapshot(status: status);
        _snapshots.GetByIdAsync(snapshot.Id, Arg.Any<CancellationToken>()).Returns(snapshot);

        var result = await Handler().ExecuteAsync(
            new OpenSaleCommand(snapshot.Id, SalesFactory.ValidCpf, null),
            Ct);

        result.Error!.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe(ErrorCodes.VehicleNotAvailable);
    }

    [Fact]
    public async Task RefusesASecondPurchaseWhileOneIsStillPending()
    {
        var snapshot = SalesFactory.Snapshot();
        _snapshots.GetByIdAsync(snapshot.Id, Arg.Any<CancellationToken>()).Returns(snapshot);
        _sales.GetPendingByVehicleAsync(snapshot.Id, Arg.Any<CancellationToken>())
            .Returns(SalesFactory.PendingSale(snapshot.Id));

        var result = await Handler().ExecuteAsync(
            new OpenSaleCommand(snapshot.Id, SalesFactory.OtherValidCpf, null),
            Ct);

        result.Error!.Code.ShouldBe(ErrorCodes.SaleAlreadyPending);
        _sales.DidNotReceive().Add(Arg.Any<Sale>());
    }

    [Fact]
    public async Task CancelsTheSaleWhenTheCatalogueDoesNotKnowTheVehicle()
    {
        var snapshot = GivenAvailableVehicle();
        Sale? added = null;
        _sales.When(s => s.Add(Arg.Any<Sale>())).Do(call => added = call.Arg<Sale>());
        _catalogue.ReserveAsync(snapshot.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CatalogueReservation.VehicleNotFound("gone"));

        var result = await Handler().ExecuteAsync(
            new OpenSaleCommand(snapshot.Id, SalesFactory.ValidCpf, null),
            Ct);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCodes.VehicleNotInCatalogue);

        added.ShouldNotBeNull().Status.ShouldBe(SaleStatus.Cancelled);
        snapshot.Status.ShouldBe(VehicleAvailability.Available);
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _catalogue.DidNotReceive()
            .TryReleaseAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelsTheSaleAndKeepsTheVehicleHeldWhenSomeoneElseGotThere()
    {
        var snapshot = GivenAvailableVehicle();
        Sale? added = null;
        _sales.When(s => s.Add(Arg.Any<Sale>())).Do(call => added = call.Arg<Sale>());
        _catalogue.ReserveAsync(snapshot.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CatalogueReservation.VehicleUnavailable("already reserved"));

        var result = await Handler().ExecuteAsync(
            new OpenSaleCommand(snapshot.Id, SalesFactory.ValidCpf, null),
            Ct);

        result.Error!.Code.ShouldBe(ErrorCodes.VehicleNotAvailable);
        added.ShouldNotBeNull().Status.ShouldBe(SaleStatus.Cancelled);
        snapshot.Status.ShouldBe(VehicleAvailability.Reserved);
    }

    [Fact]
    public async Task CompensatesWithAReleaseWhenTheCatalogueAnswerIsAmbiguous()
    {
        var snapshot = GivenAvailableVehicle();
        Sale? added = null;
        _sales.When(s => s.Add(Arg.Any<Sale>())).Do(call => added = call.Arg<Sale>());
        _catalogue.ReserveAsync(snapshot.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CatalogueReservation.CatalogueUnavailable("timeout"));
        _catalogue.TryReleaseAsync(snapshot.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await Handler().ExecuteAsync(
            new OpenSaleCommand(snapshot.Id, SalesFactory.ValidCpf, null),
            Ct);

        result.Error!.Code.ShouldBe(ErrorCodes.CatalogueUnavailable);
        added.ShouldNotBeNull().Status.ShouldBe(SaleStatus.Cancelled);
        snapshot.Status.ShouldBe(VehicleAvailability.Available);

        // The hold may have landed before the connection dropped, so it has to be dropped.
        await _catalogue.Received(1).TryReleaseAsync(snapshot.Id, "PAY-TEST-0001", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectsANullCommand()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => Handler().ExecuteAsync(null!, Ct));
    }
}
