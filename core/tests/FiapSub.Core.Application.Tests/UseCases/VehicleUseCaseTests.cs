using FiapSub.Core.Application.Abstractions;
using FiapSub.Core.Application.Common;
using FiapSub.Core.Application.Contracts;
using FiapSub.Core.Application.Tests.TestDoubles;
using FiapSub.Core.Application.UseCases.Vehicles.ExportVehicles;
using FiapSub.Core.Application.UseCases.Vehicles.GetVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.ListVehicles;
using FiapSub.Core.Application.UseCases.Vehicles.RegisterVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.ReleaseVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.ReserveVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.UpdateVehicle;
using FiapSub.Core.Domain.Exceptions;
using FiapSub.Core.Domain.ValueObjects;
using FiapSub.Core.Domain.Vehicles;

namespace FiapSub.Core.Application.Tests.UseCases;

public class VehicleUseCaseTests
{
    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedClock _clock = FixedClock.AtDefault();

    [Fact]
    public async Task RegisterVehicle_PersistsAndReturnsTheVehicle()
    {
        var handler = new RegisterVehicleHandler(_vehicles, _unitOfWork, _clock);

        var result = await handler.ExecuteAsync(
            new RegisterVehicleCommand("Fiat", "Argo Drive", 2023, "Vermelho", 78_900.00m),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Brand.ShouldBe("Fiat");
        result.Value.Status.ShouldBe(nameof(VehicleStatus.Available));
        result.Value.Price.ShouldBe(78_900.00m);
        result.Value.Currency.ShouldBe("BRL");
        result.Value.Version.ShouldBe(1);

        _vehicles.Received(1).Add(Arg.Is<Vehicle>(v => v.Id == result.Value.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterVehicle_LetsDomainValidationBubbleUp()
    {
        var handler = new RegisterVehicleHandler(_vehicles, _unitOfWork, _clock);

        await Should.ThrowAsync<DomainValidationException>(() => handler.ExecuteAsync(
            new RegisterVehicleCommand("Fiat", "Argo", 2023, "Vermelho", 0m),
            TestContext.Current.CancellationToken));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterVehicle_RejectsANullCommand()
    {
        var handler = new RegisterVehicleHandler(_vehicles, _unitOfWork, _clock);

        await Should.ThrowAsync<ArgumentNullException>(
            () => handler.ExecuteAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateVehicle_AppliesTheNewData()
    {
        var vehicle = VehicleFactory.Available();
        _vehicles.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);
        _clock.UtcNow = FixedClock.Default.AddDays(1);
        var handler = new UpdateVehicleHandler(_vehicles, _unitOfWork, _clock);

        var result = await handler.ExecuteAsync(
            new UpdateVehicleCommand(vehicle.Id, "Volkswagen", "Polo Track", 2024, "Prata", 92_000m),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Brand.ShouldBe("Volkswagen");
        result.Value.Version.ShouldBe(2);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateVehicle_ReportsNotFound()
    {
        _vehicles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Vehicle?)null);
        var handler = new UpdateVehicleHandler(_vehicles, _unitOfWork, _clock);

        var result = await handler.ExecuteAsync(
            new UpdateVehicleCommand(Guid.CreateVersion7(), "Fiat", "Argo", 2023, "Azul", 1m),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error!.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCodes.VehicleNotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVehicle_ReturnsTheVehicle()
    {
        var vehicle = VehicleFactory.Available();
        _vehicles.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await new GetVehicleHandler(_vehicles).ExecuteAsync(
            new GetVehicleQuery(vehicle.Id),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(vehicle.Id);
    }

    [Fact]
    public async Task GetVehicle_ReportsNotFound()
    {
        _vehicles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        var result = await new GetVehicleHandler(_vehicles).ExecuteAsync(
            new GetVehicleQuery(Guid.CreateVersion7()),
            TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task ListVehicles_PagesTheCatalogue()
    {
        var vehicles = new[] { VehicleFactory.Available(10m), VehicleFactory.Available(20m) };
        _vehicles.CountAsync(VehicleStatus.Available, Arg.Any<CancellationToken>()).Returns(42);
        _vehicles.ListAsync(VehicleStatus.Available, 20, 20, Arg.Any<CancellationToken>()).Returns(vehicles);

        var result = await new ListVehiclesHandler(_vehicles).ExecuteAsync(
            new ListVehiclesQuery(VehicleStatus.Available, Page: 2, PageSize: 20),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.TotalCount.ShouldBe(42);
        result.Value.Page.ShouldBe(2);
        result.Value.TotalPages.ShouldBe(3);
        result.Value.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public async Task ExportVehicles_ReturnsProjectionMessagesForTheWholeCatalogue()
    {
        var vehicle = VehicleFactory.Available();
        _vehicles.CountAsync(null, Arg.Any<CancellationToken>()).Returns(1);
        _vehicles.ListAsync(null, 0, 100, Arg.Any<CancellationToken>()).Returns(new[] { vehicle });

        var result = await new ExportVehiclesHandler(_vehicles).ExecuteAsync(
            new ExportVehiclesQuery(),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var message = result.Value.Items.ShouldHaveSingleItem();
        message.VehicleId.ShouldBe(vehicle.Id);
        message.Status.ShouldBe(nameof(VehicleStatus.Available));
        message.Version.ShouldBe(1);
    }

    [Fact]
    public async Task ReserveVehicle_HoldsTheVehicleAgainstThePaymentCode()
    {
        var vehicle = VehicleFactory.Available();
        _vehicles.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);
        var handler = new ReserveVehicleHandler(_vehicles, _unitOfWork, _clock);

        var result = await handler.ExecuteAsync(
            new ReserveVehicleCommand(vehicle.Id, "PAY-1"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(nameof(VehicleStatus.Reserved));
        result.Value.PaymentCode.ShouldBe("PAY-1");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReserveVehicle_ReportsNotFound()
    {
        _vehicles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Vehicle?)null);
        var handler = new ReserveVehicleHandler(_vehicles, _unitOfWork, _clock);

        var result = await handler.ExecuteAsync(
            new ReserveVehicleCommand(Guid.CreateVersion7(), "PAY-1"),
            TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task ReserveVehicle_SurfacesTheConflictWhenTheVehicleIsTaken()
    {
        var vehicle = VehicleFactory.Reserved(PaymentCode.Create("PAY-OTHER"));
        _vehicles.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);
        var handler = new ReserveVehicleHandler(_vehicles, _unitOfWork, _clock);

        await Should.ThrowAsync<DomainStateException>(() => handler.ExecuteAsync(
            new ReserveVehicleCommand(vehicle.Id, "PAY-1"),
            TestContext.Current.CancellationToken));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseVehicle_PutsTheVehicleBackOnTheShelf()
    {
        var vehicle = VehicleFactory.Reserved(PaymentCode.Create("PAY-1"));
        _vehicles.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);
        var handler = new ReleaseVehicleHandler(_vehicles, _unitOfWork, _clock);

        var result = await handler.ExecuteAsync(
            new ReleaseVehicleCommand(vehicle.Id, "PAY-1"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(nameof(VehicleStatus.Available));
        result.Value.PaymentCode.ShouldBeNull();
    }

    [Fact]
    public async Task ReleaseVehicle_ReportsNotFound()
    {
        _vehicles.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Vehicle?)null);
        var handler = new ReleaseVehicleHandler(_vehicles, _unitOfWork, _clock);

        var result = await handler.ExecuteAsync(
            new ReleaseVehicleCommand(Guid.CreateVersion7(), "PAY-1"),
            TestContext.Current.CancellationToken);

        result.Error!.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task VehicleResponse_RejectsANullVehicle()
    {
        await Task.CompletedTask;
        Should.Throw<ArgumentNullException>(() => VehicleResponse.From(null!));
    }
}
