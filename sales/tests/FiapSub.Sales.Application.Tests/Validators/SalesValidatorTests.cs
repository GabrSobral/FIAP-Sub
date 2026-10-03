using FiapSub.Sales.Application.Tests.TestDoubles;
using FiapSub.Sales.Application.UseCases.Catalogue.SyncCatalogue;
using FiapSub.Sales.Application.UseCases.Catalogue.UpsertVehicleSnapshot;
using FiapSub.Sales.Application.UseCases.Listings.ListSoldVehicles;
using FiapSub.Sales.Application.UseCases.Listings.ListVehiclesForSale;
using FiapSub.Sales.Application.UseCases.Payments.ApplyPaymentResult;
using FiapSub.Sales.Application.UseCases.Sales.OpenSale;

namespace FiapSub.Sales.Application.Tests.Validators;

public class SalesValidatorTests
{
    [Theory]
    [InlineData(1, 20, true)]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void Listings_BoundThePaging(int page, int pageSize, bool expected)
    {
        new ListVehiclesForSaleValidator()
            .Validate(new ListVehiclesForSaleQuery(page, pageSize)).IsValid.ShouldBe(expected);

        new ListSoldVehiclesValidator()
            .Validate(new ListSoldVehiclesQuery(page, pageSize)).IsValid.ShouldBe(expected);
    }

    [Fact]
    public void OpenSale_RequiresAVehicleAndAnElevenDigitCpf()
    {
        var validator = new OpenSaleValidator();
        var vehicleId = Guid.CreateVersion7();

        validator.Validate(new OpenSaleCommand(vehicleId, SalesFactory.ValidCpf, null)).IsValid.ShouldBeTrue();
        validator.Validate(new OpenSaleCommand(vehicleId, "52998224725", null)).IsValid.ShouldBeTrue();
        validator.Validate(new OpenSaleCommand(Guid.Empty, SalesFactory.ValidCpf, null)).IsValid.ShouldBeFalse();
        validator.Validate(new OpenSaleCommand(vehicleId, null, null)).IsValid.ShouldBeFalse();
        validator.Validate(new OpenSaleCommand(vehicleId, "123", null)).IsValid.ShouldBeFalse();
        validator.Validate(new OpenSaleCommand(vehicleId, "529982247250", null)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void UpsertVehicleSnapshot_RequiresTheWholePayload()
    {
        var validator = new UpsertVehicleSnapshotValidator();
        var valid = new UpsertVehicleSnapshotCommand(
            Guid.CreateVersion7(), "Fiat", "Argo", 2023, "Vermelho", 1m, "BRL", "Available", 1, FixedClock.Default);

        validator.Validate(valid).IsValid.ShouldBeTrue();
        validator.Validate(valid with { VehicleId = Guid.Empty }).IsValid.ShouldBeFalse();
        validator.Validate(valid with { Brand = "" }).IsValid.ShouldBeFalse();
        validator.Validate(valid with { Model = null }).IsValid.ShouldBeFalse();
        validator.Validate(valid with { Color = " " }).IsValid.ShouldBeFalse();
        validator.Validate(valid with { Status = "" }).IsValid.ShouldBeFalse();
        validator.Validate(valid with { Price = 0m }).IsValid.ShouldBeFalse();
        validator.Validate(valid with { Version = 0 }).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void ApplyPaymentResult_RequiresACodeAndAStatus()
    {
        var validator = new ApplyPaymentResultValidator();

        validator.Validate(new ApplyPaymentResultCommand("PAY-1", "approved")).IsValid.ShouldBeTrue();
        validator.Validate(new ApplyPaymentResultCommand("", "approved")).IsValid.ShouldBeFalse();
        validator.Validate(new ApplyPaymentResultCommand("PAY-1", null)).IsValid.ShouldBeFalse();
        validator.Validate(new ApplyPaymentResultCommand(new string('x', 65), "approved")).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(500, true)]
    [InlineData(501, false)]
    [InlineData(0, false)]
    public void SyncCatalogue_BoundsThePageSize(int pageSize, bool expected)
    {
        new SyncCatalogueValidator().Validate(new SyncCatalogueCommand(pageSize)).IsValid.ShouldBe(expected);
    }
}
