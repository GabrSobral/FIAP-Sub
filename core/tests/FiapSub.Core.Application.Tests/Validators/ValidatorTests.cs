using FiapSub.Core.Application.UseCases.Payments.ProcessPaymentWebhook;
using FiapSub.Core.Application.UseCases.Vehicles.ExportVehicles;
using FiapSub.Core.Application.UseCases.Vehicles.ListVehicles;
using FiapSub.Core.Application.UseCases.Vehicles.RegisterVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.ReleaseVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.ReserveVehicle;
using FiapSub.Core.Application.UseCases.Vehicles.UpdateVehicle;
using FiapSub.Core.Domain.Payments;

namespace FiapSub.Core.Application.Tests.Validators;

public class ValidatorTests
{
    private static readonly RegisterVehicleCommand ValidRegister =
        new("Fiat", "Argo Drive", 2023, "Vermelho", 78_900m);

    [Fact]
    public void RegisterVehicle_AcceptsAWellFormedCommand()
    {
        new RegisterVehicleValidator().Validate(ValidRegister).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "Argo", "Vermelho", 2023, 1)]
    [InlineData("Fiat", "", "Vermelho", 2023, 1)]
    [InlineData("Fiat", "Argo", "", 2023, 1)]
    [InlineData("Fiat", "Argo", "Vermelho", 1899, 1)]
    [InlineData("Fiat", "Argo", "Vermelho", 2023, 0)]
    [InlineData("Fiat", "Argo", "Vermelho", 2023, -5)]
    public void RegisterVehicle_RejectsBadInput(string brand, string model, string color, int year, decimal price)
    {
        new RegisterVehicleValidator()
            .Validate(new RegisterVehicleCommand(brand, model, year, color, price))
            .IsValid.ShouldBeFalse();
    }

    [Fact]
    public void UpdateVehicle_RequiresAnId()
    {
        new UpdateVehicleValidator()
            .Validate(new UpdateVehicleCommand(Guid.Empty, "Fiat", "Argo", 2023, "Vermelho", 1m))
            .IsValid.ShouldBeFalse();

        new UpdateVehicleValidator()
            .Validate(new UpdateVehicleCommand(Guid.CreateVersion7(), "Fiat", "Argo", 2023, "Vermelho", 1m))
            .IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1, 20, true)]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void ListVehicles_BoundsThePaging(int page, int pageSize, bool expected)
    {
        new ListVehiclesValidator()
            .Validate(new ListVehiclesQuery(null, page, pageSize))
            .IsValid.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, 100, true)]
    [InlineData(1, 501, false)]
    [InlineData(0, 100, false)]
    public void ExportVehicles_BoundsThePaging(int page, int pageSize, bool expected)
    {
        new ExportVehiclesValidator()
            .Validate(new ExportVehiclesQuery(page, pageSize))
            .IsValid.ShouldBe(expected);
    }

    [Fact]
    public void ReserveVehicle_RequiresIdAndPaymentCode()
    {
        var validator = new ReserveVehicleValidator();

        validator.Validate(new ReserveVehicleCommand(Guid.CreateVersion7(), "PAY-1")).IsValid.ShouldBeTrue();
        validator.Validate(new ReserveVehicleCommand(Guid.Empty, "PAY-1")).IsValid.ShouldBeFalse();
        validator.Validate(new ReserveVehicleCommand(Guid.CreateVersion7(), "")).IsValid.ShouldBeFalse();
        validator.Validate(new ReserveVehicleCommand(Guid.CreateVersion7(), new string('x', 65))).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void ReleaseVehicle_RequiresIdAndPaymentCode()
    {
        var validator = new ReleaseVehicleValidator();

        validator.Validate(new ReleaseVehicleCommand(Guid.CreateVersion7(), "PAY-1")).IsValid.ShouldBeTrue();
        validator.Validate(new ReleaseVehicleCommand(Guid.Empty, null)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void ProcessPaymentWebhook_RequiresACodeAndAKnownOutcome()
    {
        var validator = new ProcessPaymentWebhookValidator();

        validator.Validate(new ProcessPaymentWebhookCommand("PAY-1", PaymentOutcome.Approved)).IsValid.ShouldBeTrue();
        validator.Validate(new ProcessPaymentWebhookCommand("", PaymentOutcome.Approved)).IsValid.ShouldBeFalse();
        validator.Validate(new ProcessPaymentWebhookCommand("PAY-1", (PaymentOutcome)99)).IsValid.ShouldBeFalse();
    }
}
