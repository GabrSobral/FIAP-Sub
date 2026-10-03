using FiapSub.Sales.Domain.Catalog;
using FiapSub.Sales.Domain.Exceptions;

namespace FiapSub.Sales.Domain.Tests.Catalog;

public class VehicleSnapshotTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);

    private static VehicleSnapshot Replicate(
        long version = 1,
        VehicleAvailability status = VehicleAvailability.Available,
        decimal price = 78_900.00m) =>
        VehicleSnapshot.Replicate(
            Guid.CreateVersion7(),
            " Fiat ",
            " Argo Drive ",
            2023,
            " Vermelho ",
            price,
            "brl",
            status,
            version,
            Now,
            Now);

    [Fact]
    public void Replicate_CopiesTheCatalogueStateAndTrimsText()
    {
        var snapshot = Replicate();

        snapshot.Brand.ShouldBe("Fiat");
        snapshot.Model.ShouldBe("Argo Drive");
        snapshot.Color.ShouldBe("Vermelho");
        snapshot.Currency.ShouldBe("BRL");
        snapshot.Year.ShouldBe(2023);
        snapshot.Price.ShouldBe(78_900.00m);
        snapshot.Status.ShouldBe(VehicleAvailability.Available);
        snapshot.IsForSale.ShouldBeTrue();
        snapshot.Version.ShouldBe(1);
        snapshot.UpdatedAt.ShouldBe(Now);
        snapshot.ReplicatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Replicate_RejectsAnEmptyVehicleId()
    {
        Should.Throw<DomainValidationException>(() => VehicleSnapshot.Replicate(
                Guid.Empty, "Fiat", "Argo", 2023, "Vermelho", 1m, "BRL", VehicleAvailability.Available, 1, Now, Now))
            .Message.ShouldContain("Vehicle id is required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Replicate_RejectsANonPositiveVersion(long version)
    {
        Should.Throw<DomainValidationException>(() => Replicate(version))
            .Message.ShouldContain("version must be positive");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Replicate_RejectsANonPositivePrice(decimal price)
    {
        Should.Throw<DomainValidationException>(() => Replicate(price: price))
            .Message.ShouldContain("price must be greater than zero");
    }

    [Fact]
    public void Replicate_RejectsAnUnknownStatus()
    {
        Should.Throw<DomainValidationException>(() => Replicate(status: (VehicleAvailability)99))
            .Message.ShouldContain("Unknown vehicle status");
    }

    [Fact]
    public void Replicate_RejectsBlankText()
    {
        Should.Throw<DomainValidationException>(() => VehicleSnapshot.Replicate(
            Guid.CreateVersion7(), "", "Argo", 2023, "Vermelho", 1m, "BRL", VehicleAvailability.Available, 1, Now, Now));
    }

    [Fact]
    public void TryApplyUpdate_AppliesANewerVersion()
    {
        var snapshot = Replicate();
        var later = Now.AddMinutes(5);

        var changed = snapshot.TryApplyUpdate(
            "Volkswagen", "Polo Track", 2024, "Prata", 92_000m, "BRL",
            VehicleAvailability.Reserved, 2, later, later);

        changed.ShouldBeTrue();
        snapshot.Brand.ShouldBe("Volkswagen");
        snapshot.Status.ShouldBe(VehicleAvailability.Reserved);
        snapshot.IsForSale.ShouldBeFalse();
        snapshot.Price.ShouldBe(92_000m);
        snapshot.Version.ShouldBe(2);
        snapshot.UpdatedAt.ShouldBe(later);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public void TryApplyUpdate_IgnoresAnUpdateThatIsNotNewer(long version)
    {
        var snapshot = Replicate(version: 3);
        snapshot.TryApplyUpdate(
            "Volkswagen", "Polo", 2024, "Prata", 1m, "BRL",
            VehicleAvailability.Sold, version, Now, Now).ShouldBeFalse();

        snapshot.Brand.ShouldBe("Fiat");
        snapshot.Version.ShouldBe(3);
        snapshot.Status.ShouldBe(VehicleAvailability.Available);
    }

    [Fact]
    public void TryApplyUpdate_IgnoresTheSameVersionReplayedOutOfOrder()
    {
        var snapshot = Replicate(version: 2);

        snapshot.TryApplyUpdate(
            "Fiat", "Argo", 2023, "Vermelho", 1m, "BRL",
            VehicleAvailability.Sold, 2, Now, Now).ShouldBeFalse();

        snapshot.Status.ShouldBe(VehicleAvailability.Available);
    }

    [Fact]
    public void TryApplyUpdate_StillValidatesTheIncomingPayload()
    {
        var snapshot = Replicate();

        Should.Throw<DomainValidationException>(() => snapshot.TryApplyUpdate(
            "Fiat", "Argo", 2023, "Vermelho", -5m, "BRL", VehicleAvailability.Sold, 2, Now, Now));
    }

    [Fact]
    public void ProjectLocalStatus_ChangesTheStatusWithoutClaimingANewVersion()
    {
        var snapshot = Replicate(version: 4);
        var later = Now.AddSeconds(30);

        snapshot.ProjectLocalStatus(VehicleAvailability.Reserved, later);

        snapshot.Status.ShouldBe(VehicleAvailability.Reserved);
        snapshot.Version.ShouldBe(4);
        snapshot.ReplicatedAt.ShouldBe(later);
    }

    [Fact]
    public void ProjectLocalStatus_IsOverriddenByTheNextCataloguePush()
    {
        var snapshot = Replicate(version: 1);
        snapshot.ProjectLocalStatus(VehicleAvailability.Reserved, Now);

        snapshot.TryApplyUpdate(
            "Fiat", "Argo", 2023, "Vermelho", 78_900m, "BRL",
            VehicleAvailability.Available, 2, Now, Now).ShouldBeTrue();

        snapshot.Status.ShouldBe(VehicleAvailability.Available);
    }

    [Fact]
    public void ProjectLocalStatus_RejectsAnUnknownStatus()
    {
        Should.Throw<DomainValidationException>(
            () => Replicate().ProjectLocalStatus((VehicleAvailability)42, Now));
    }
}
