using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Application.Common;
using FiapSub.Sales.Application.Gateways;
using FiapSub.Sales.Application.Tests.TestDoubles;
using FiapSub.Sales.Application.UseCases.Catalogue.SyncCatalogue;
using FiapSub.Sales.Application.UseCases.Catalogue.UpsertVehicleSnapshot;
using FiapSub.Sales.Domain.Catalog;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapSub.Sales.Application.Tests.UseCases;

public class CatalogueProjectionTests
{
    private readonly IVehicleSnapshotRepository _snapshots = Substitute.For<IVehicleSnapshotRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedClock _clock = FixedClock.AtDefault();

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    private UpsertVehicleSnapshotHandler Upsert() => new(_snapshots, _unitOfWork, _clock);

    private static UpsertVehicleSnapshotCommand Push(
        Guid vehicleId,
        long version = 1,
        string status = "Available",
        decimal price = 78_900m) =>
        new(vehicleId, "Fiat", "Argo Drive", 2023, "Vermelho", price, "BRL", status, version, FixedClock.Default);

    [Fact]
    public async Task FirstPushCreatesTheReplica()
    {
        var vehicleId = Guid.CreateVersion7();
        _snapshots.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>()).Returns((VehicleSnapshot?)null);

        var result = await Upsert().ExecuteAsync(Push(vehicleId), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Applied.ShouldBeTrue();
        result.Value.VehicleId.ShouldBe(vehicleId);
        result.Value.Status.ShouldBe(nameof(VehicleAvailability.Available));

        _snapshots.Received(1).Add(Arg.Is<VehicleSnapshot>(s => s.Id == vehicleId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ANewerPushUpdatesTheReplica()
    {
        var existing = SalesFactory.Snapshot(version: 1);
        _snapshots.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await Upsert().ExecuteAsync(Push(existing.Id, version: 2, status: "Sold"), Ct);

        result.Value.Applied.ShouldBeTrue();
        result.Value.Version.ShouldBe(2);
        existing.Status.ShouldBe(VehicleAvailability.Sold);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AReplayedOrStalePushIsIgnoredAndNothingIsWritten()
    {
        var existing = SalesFactory.Snapshot(version: 5);
        _snapshots.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await Upsert().ExecuteAsync(Push(existing.Id, version: 3, status: "Sold"), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Applied.ShouldBeFalse();
        result.Value.Version.ShouldBe(5);
        existing.Status.ShouldBe(VehicleAvailability.Available);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("available")]
    [InlineData("RESERVED")]
    [InlineData("Sold")]
    public async Task StatusParsingIsCaseInsensitive(string status)
    {
        var vehicleId = Guid.CreateVersion7();
        _snapshots.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>()).Returns((VehicleSnapshot?)null);

        (await Upsert().ExecuteAsync(Push(vehicleId, status: status), Ct)).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Scrapped")]
    [InlineData("")]
    [InlineData("4")]
    public async Task AnUnknownStatusIsAValidationFailure(string status)
    {
        var result = await Upsert().ExecuteAsync(Push(Guid.CreateVersion7(), status: status), Ct);

        result.Error!.Type.ShouldBe(ErrorType.Validation);
        result.Error.Message.ShouldContain("Unknown vehicle status");
    }

    [Fact]
    public async Task UpsertRejectsANullCommand()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => Upsert().ExecuteAsync(null!, Ct));
    }

    [Fact]
    public async Task SyncCataloguePullsEveryPageAndCountsWhatItApplied()
    {
        var catalogue = Substitute.For<IVehicleCatalogueGateway>();
        var upsert = Substitute.For<IUseCase<UpsertVehicleSnapshotCommand, VehicleSnapshotAck>>();

        var first = Enumerable.Range(0, 2).Select(_ => Vehicle()).ToList();
        var second = new List<CatalogueVehicle> { Vehicle() };

        catalogue.ExportAsync(1, 2, Arg.Any<CancellationToken>()).Returns(first);
        catalogue.ExportAsync(2, 2, Arg.Any<CancellationToken>()).Returns(second);

        upsert.ExecuteAsync(Arg.Any<UpsertVehicleSnapshotCommand>(), Arg.Any<CancellationToken>())
            .Returns(call => Result.Success(new VehicleSnapshotAck(
                call.Arg<UpsertVehicleSnapshotCommand>().VehicleId, 1, Applied: true, "Available")));

        var result = await new SyncCatalogueHandler(catalogue, upsert, NullLogger<SyncCatalogueHandler>.Instance)
            .ExecuteAsync(new SyncCatalogueCommand(PageSize: 2), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Pulled.ShouldBe(3);
        result.Value.Applied.ShouldBe(3);
        await catalogue.DidNotReceive().ExportAsync(3, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncCatalogueSkipsVehiclesTheProjectionRefuses()
    {
        var catalogue = Substitute.For<IVehicleCatalogueGateway>();
        var upsert = Substitute.For<IUseCase<UpsertVehicleSnapshotCommand, VehicleSnapshotAck>>();

        catalogue.ExportAsync(1, 100, Arg.Any<CancellationToken>()).Returns([Vehicle(), Vehicle()]);
        upsert.ExecuteAsync(Arg.Any<UpsertVehicleSnapshotCommand>(), Arg.Any<CancellationToken>())
            .Returns(
                Result.Failure<VehicleSnapshotAck>(Error.Validation("bad", "nope")),
                Result.Success(new VehicleSnapshotAck(Guid.CreateVersion7(), 1, Applied: false, "Available")));

        var result = await new SyncCatalogueHandler(catalogue, upsert, NullLogger<SyncCatalogueHandler>.Instance)
            .ExecuteAsync(new SyncCatalogueCommand(), Ct);

        result.Value.Pulled.ShouldBe(2);
        result.Value.Applied.ShouldBe(0);
    }

    [Fact]
    public async Task SyncCatalogueRejectsANullCommand()
    {
        var handler = new SyncCatalogueHandler(
            Substitute.For<IVehicleCatalogueGateway>(),
            Substitute.For<IUseCase<UpsertVehicleSnapshotCommand, VehicleSnapshotAck>>(),
            NullLogger<SyncCatalogueHandler>.Instance);

        await Should.ThrowAsync<ArgumentNullException>(() => handler.ExecuteAsync(null!, Ct));
    }

    private static CatalogueVehicle Vehicle() => new(
        Guid.CreateVersion7(), "Fiat", "Argo", 2023, "Vermelho", 78_900m, "BRL", "Available", 1, FixedClock.Default);
}
