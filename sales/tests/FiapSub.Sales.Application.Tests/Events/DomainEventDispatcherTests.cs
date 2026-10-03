using FiapSub.Sales.Application.Abstractions.Events;
using FiapSub.Sales.Domain.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapSub.Sales.Application.Tests.Events;

public class DomainEventDispatcherTests
{
    private sealed record Alpha(DateTimeOffset OccurredOn) : IDomainEvent;

    private sealed record Beta(DateTimeOffset OccurredOn) : IDomainEvent;

    private sealed class RecordingHandler<T> : DomainEventHandler<T>
        where T : IDomainEvent
    {
        public int Calls { get; private set; }

        protected override Task HandleAsync(T domainEvent, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IDomainEventHandler
    {
        public bool CanHandle(IDomainEvent domainEvent) => true;

        public Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("the sales service is down");
    }

    private static DomainEventDispatcher Dispatcher(params IDomainEventHandler[] handlers) =>
        new(handlers, NullLogger<DomainEventDispatcher>.Instance);

    [Fact]
    public async Task EachEventGoesOnlyToTheHandlersThatClaimIt()
    {
        var alphaHandler = new RecordingHandler<Alpha>();
        var betaHandler = new RecordingHandler<Beta>();

        await Dispatcher(alphaHandler, betaHandler).DispatchAsync(
            [new Alpha(DateTimeOffset.UnixEpoch), new Alpha(DateTimeOffset.UnixEpoch), new Beta(DateTimeOffset.UnixEpoch)],
            TestContext.Current.CancellationToken);

        alphaHandler.Calls.ShouldBe(2);
        betaHandler.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task AFailingHandlerIsLoggedAndDoesNotStopTheOthers()
    {
        var survivor = new RecordingHandler<Alpha>();

        await Dispatcher(new ThrowingHandler(), survivor).DispatchAsync(
            [new Alpha(DateTimeOffset.UnixEpoch)],
            TestContext.Current.CancellationToken);

        survivor.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task NullEventCollectionIsRejected()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            () => Dispatcher().DispatchAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task NoHandlersMeansNothingHappens()
    {
        await Dispatcher().DispatchAsync([new Alpha(DateTimeOffset.UnixEpoch)], TestContext.Current.CancellationToken);
    }
}
