namespace FiapSub.Core.Domain.Abstractions;

/// <summary>Identity-based equality: two entities are the same when their ids match.</summary>
public abstract class Entity<TId>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    /// <summary>Required by EF Core when materialising from the database.</summary>
    protected Entity() => Id = default!;

    public TId Id { get; protected set; }

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other
        && other.GetType() == GetType()
        && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
