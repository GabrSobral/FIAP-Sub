using FiapSub.Core.Domain.Abstractions;

namespace FiapSub.Core.Domain.Tests.Abstractions;

public class AggregateRootTests
{
    private sealed record Happened(DateTimeOffset OccurredOn) : IDomainEvent;

    private sealed class Thing : AggregateRoot<Guid>
    {
        public Thing(Guid id) : base(id)
        {
        }

        public void Do(DateTimeOffset at) => Raise(new Happened(at));
    }

    private sealed class OtherThing : Entity<Guid>
    {
        public OtherThing(Guid id) : base(id)
        {
        }
    }

    [Fact]
    public void RaisedEventsAreExposedAndClearable()
    {
        var thing = new Thing(Guid.CreateVersion7());
        var at = DateTimeOffset.UnixEpoch;

        thing.DomainEvents.ShouldBeEmpty();

        thing.Do(at);
        thing.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<Happened>().OccurredOn.ShouldBe(at);

        thing.ClearDomainEvents();
        thing.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void EntityEquality_IsByIdAndConcreteType()
    {
        var id = Guid.CreateVersion7();

        new Thing(id).Equals(new Thing(id)).ShouldBeTrue();
        new Thing(id).GetHashCode().ShouldBe(new Thing(id).GetHashCode());
        new Thing(id).Equals(new Thing(Guid.CreateVersion7())).ShouldBeFalse();
        new Thing(id).Equals(new OtherThing(id)).ShouldBeFalse();
        new Thing(id).Equals(null).ShouldBeFalse();
        new Thing(id).Equals("not an entity").ShouldBeFalse();
    }
}
