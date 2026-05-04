using System.Numerics;
using System.Reflection;

namespace Dialysis.DomainDrivenDesign.Primitives;

/// <summary>
/// Smart enum: discrete named values with stable integer ids (useful for statuses, types, etc.).
/// </summary>
public abstract class Enumeration : IComparable<Enumeration>, IEqualityOperators<Enumeration, Enumeration, bool>
{
    protected Enumeration(int value, string name)
    {
        Value = value;
        Name = name;
    }

    public int Value { get; }

    public string Name { get; }

    public int CompareTo(Enumeration? other) => Compare(this, other);

    public static bool operator <(Enumeration? left, Enumeration? right) => Compare(left, right) < 0;

    public static bool operator >(Enumeration? left, Enumeration? right) => Compare(left, right) > 0;

    public static bool operator <=(Enumeration? left, Enumeration? right) => Compare(left, right) <= 0;

    public static bool operator >=(Enumeration? left, Enumeration? right) => Compare(left, right) >= 0;

    private static int Compare(Enumeration? left, Enumeration? right) =>
        (left, right) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            _ when left.GetType() != right.GetType() =>
                throw new ArgumentException("Cannot compare enumerations of different types."),
            _ => left.Value.CompareTo(right.Value),
        };

    public override string ToString() => Name;

    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration other)
        {
            return false;
        }

        return Value == other.Value && GetType() == other.GetType();
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Value);

    public static bool operator ==(Enumeration? left, Enumeration? right) => Equals(left, right);

    public static bool operator !=(Enumeration? left, Enumeration? right) => !Equals(left, right);

    /// <summary>
    /// Returns all public static readonly fields of type <typeparamref name="T"/> declared on <typeparamref name="T"/>.
    /// </summary>
    public static IReadOnlyList<T> GetAll<T>()
        where T : Enumeration =>
        typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(T))
            .Select(f => f.GetValue(null))
            .Cast<T>()
            .ToList();
}
