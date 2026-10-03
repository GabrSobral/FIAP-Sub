using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Tests.TestDoubles;
using FiapSub.Sales.Application.UseCases.Listings.ListSoldVehicles;
using FiapSub.Sales.Application.UseCases.Listings.ListVehiclesForSale;
using FiapSub.Sales.Application.UseCases.Sales.GetSale;
using FiapSub.Sales.Domain.Catalog;
using FiapSub.Sales.Domain.Sales;

namespace FiapSub.Sales.Application.Tests.UseCases;

public class ListingAndSaleQueryTests
{
    private readonly IVehicleSnapshotRepository _snapshots = Substitute.For<IVehicleSnapshotRepository>();
    private readonly ISaleRepository _sales = Substitute.For<ISaleRepository>();

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ListVehiclesForSale_AsksTheProjectionForAvailableVehiclesOrderedByPrice()
    {
        var cheaper = SalesFactory.Snapshot(price: 30_000m);
        var dearer = SalesFactory.Snapshot(price: 90_000m);
        _snapshots.CountByStatusAsync(VehicleAvailability.Available, Arg.Any<CancellationToken>()).Returns(2);
        _snapshots.ListByStatusOrderedByPriceAsync(
                VehicleAvailability.Available, 0, 20, Arg.Any<CancellationToken>())
            .Returns([cheaper, dearer]);

        var result = await new ListVehiclesForSaleHandler(_snapshots)
            .ExecuteAsync(new ListVehiclesForSaleQuery(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Items.Select(i => i.Price).ShouldBe([30_000m, 90_000m]);
        result.Value.Items[0].Status.ShouldBe(nameof(VehicleAvailability.Available));
    }

    [Fact]
    public async Task ListVehiclesForSale_TranslatesThePageIntoASkip()
    {
        _snapshots.CountByStatusAsync(VehicleAvailability.Available, Arg.Any<CancellationToken>()).Returns(0);
        _snapshots.ListByStatusOrderedByPriceAsync(
            VehicleAvailability.Available, 20, 10, Arg.Any<CancellationToken>()).Returns([]);

        var result = await new ListVehiclesForSaleHandler(_snapshots)
            .ExecuteAsync(new ListVehiclesForSaleQuery(Page: 3, PageSize: 10), Ct);

        result.IsSuccess.ShouldBeTrue();
        await _snapshots.Received(1).ListByStatusOrderedByPriceAsync(
            VehicleAvailability.Available, 20, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListSoldVehicles_AsksTheProjectionForSoldVehicles()
    {
        var snapshot = SalesFactory.Snapshot(status: VehicleAvailability.Sold, price: 42_000m);
        _snapshots.CountByStatusAsync(VehicleAvailability.Sold, Arg.Any<CancellationToken>()).Returns(1);
        _snapshots.ListByStatusOrderedByPriceAsync(
            VehicleAvailability.Sold, 0, 20, Arg.Any<CancellationToken>()).Returns([snapshot]);

        var result = await new ListSoldVehiclesHandler(_snapshots)
            .ExecuteAsync(new ListSoldVehiclesQuery(), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldHaveSingleItem().Status.ShouldBe(nameof(VehicleAvailability.Sold));
    }

    [Fact]
    public async Task Listings_RejectANullQuery()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            () => new ListVehiclesForSaleHandler(_snapshots).ExecuteAsync(null!, Ct));
        await Should.ThrowAsync<ArgumentNullException>(
            () => new ListSoldVehiclesHandler(_snapshots).ExecuteAsync(null!, Ct));
    }

    [Fact]
    public async Task GetSale_ReturnsTheSaleWithTheCpfMasked()
    {
        var sale = SalesFactory.PendingSale(Guid.CreateVersion7());
        _sales.GetByIdAsync(sale.Id, Arg.Any<CancellationToken>()).Returns(sale);

        var result = await new GetSaleHandler(_sales).ExecuteAsync(new GetSaleQuery(sale.Id), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(sale.Id);
        result.Value.BuyerCpf.ShouldBe("529.982.247-25");
    }

    [Fact]
    public async Task GetSale_ReportsNotFound()
    {
        _sales.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var result = await new GetSaleHandler(_sales).ExecuteAsync(new GetSaleQuery(Guid.CreateVersion7()), Ct);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCodes.SaleNotFound);
    }

    [Fact]
    public async Task GetSale_RejectsANullQuery()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => new GetSaleHandler(_sales).ExecuteAsync(null!, Ct));
    }
}
