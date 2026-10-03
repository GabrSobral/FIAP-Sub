using FiapSub.Sales.Application.EventHandlers;
using FiapSub.Sales.Application.Tests.TestDoubles;
using FiapSub.Sales.Domain.Abstractions;
using FiapSub.Sales.Domain.Sales.Events;
using Microsoft.Extensions.Logging;

namespace FiapSub.Sales.Application.Tests.Events;

public class SaleAuditLogHandlerTests
{
    private sealed record Unrelated(DateTimeOffset OccurredOn) : IDomainEvent;

    private readonly ILogger<SaleAuditLogHandler> _logger = Substitute.For<ILogger<SaleAuditLogHandler>>();

    private SaleAuditLogHandler Handler() => new(_logger);

    [Fact]
    public void ClaimsEverySaleLifecycleEventAndNothingElse()
    {
        var handler = Handler();
        var now = FixedClock.Default;
        var saleId = Guid.CreateVersion7();
        var vehicleId = Guid.CreateVersion7();

        handler.CanHandle(new SaleOpened(saleId, vehicleId, "PAY-1", now)).ShouldBeTrue();
        handler.CanHandle(new SalePaid(saleId, vehicleId, now, now)).ShouldBeTrue();
        handler.CanHandle(new SaleCancelled(saleId, vehicleId, "refused", now)).ShouldBeTrue();
        handler.CanHandle(new Unrelated(now)).ShouldBeFalse();
    }

    [Fact]
    public async Task WritesOneAuditLineForEachTransition()
    {
        var handler = Handler();
        var now = FixedClock.Default;
        var saleId = Guid.CreateVersion7();
        var vehicleId = Guid.CreateVersion7();
        var ct = TestContext.Current.CancellationToken;

        await handler.HandleAsync(new SaleOpened(saleId, vehicleId, "PAY-1", now), ct);
        await handler.HandleAsync(new SalePaid(saleId, vehicleId, now, now), ct);
        await handler.HandleAsync(new SaleCancelled(saleId, vehicleId, "refused", now), ct);
        await handler.HandleAsync(new Unrelated(now), ct);

        _logger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).ShouldBe(3);
    }
}
