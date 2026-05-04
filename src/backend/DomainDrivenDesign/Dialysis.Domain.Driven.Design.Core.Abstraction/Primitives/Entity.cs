namespace Dialysis.DomainDrivenDesign.Primitives;

/// <summary>
/// Entity equality is defined by <typeparamref name="TId"/> and concrete runtime type.
/// Use <see cref="Equals(object?)"/> or <see cref="object.Equals(object?, object?)"/> for identity; aggregate roots also expose <c>==</c> via <see cref="AggregateRoot{TId}"/>.
/// </summary>
public abstract class Entity<TId> : Audit
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    protected Entity()
    {
    }

    protected Entity(TId id) => Id = id;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
