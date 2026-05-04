using System.Numerics;

namespace Dialysis.DomainDrivenDesign.Primitives;

/// <summary>
/// Value object equality is structural: all components returned from <see cref="GetEqualityComponents"/> must match.
/// Implements <see cref="IEqualityOperators{TSelf,TOther,TResult}"/> so <c>==</c>/<c>!=</c> are part of the numeric/generic equality contract rather than a lone pair of operators on a reference type.
/// </summary>
public abstract class ValueObject : IEqualityOperators<ValueObject, ValueObject, bool>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is not ValueObject other)
        {
            return false;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(0, (hash, obj) => HashCode.Combine(hash, obj?.GetHashCode() ?? 0));

    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
}
