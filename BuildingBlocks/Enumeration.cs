using System.Reflection;

namespace BuildingBlocks;

public abstract class Enumeration<T> : IComparable
{
    public T Id { get; }
    public string Value { get; }

    protected Enumeration(T id, string value)
    {
        Id = id;
        Value = value;
    }

    public override string ToString() => $"{Id}-{Value}";
    public int CompareTo(object? obj)
    {
        return obj switch
        {
            null => 1,
            Enumeration<T> otherEnumeration => Comparer<T>.Default.Compare(Id, otherEnumeration.Id),
            _ => throw new ArgumentException($"Object is not a {GetType().Name}")
        };
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is null)
            return false;

        if (obj is not Enumeration<T> other)
            return false;

        bool typeMatches = GetType() == other.GetType();
        bool valueMatches = EqualityComparer<T>.Default.Equals(Id, other.Id);

        return typeMatches && valueMatches;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Enumeration<T> left, Enumeration<T> right) => left.Equals(right);

    public static bool operator !=(Enumeration<T> left, Enumeration<T> right) => !(left == right);

    public static bool operator <(Enumeration<T> left, Enumeration<T> right) => left.CompareTo(right) < 0;

    public static bool operator <=(Enumeration<T> left, Enumeration<T> right) => left.CompareTo(right) <= 0;

    public static bool operator >(Enumeration<T> left, Enumeration<T> right) => left.CompareTo(right) > 0;

    public static bool operator >=(Enumeration<T> left, Enumeration<T> right) => left.CompareTo(right) >= 0;
}

public class EnumerationInt : Enumeration<int>
{
    public EnumerationInt(int id, string value) : base(id, value) { }

    public static IEnumerable<T> GetAll<T>() where T : EnumerationInt =>
        typeof(T).GetFields(BindingFlags.Public |
                            BindingFlags.Static |
                            BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();

}

public class EnumerationShort : Enumeration<short>
{
    public EnumerationShort(short id, string value) : base(id, value) { }

    public static IEnumerable<T> GetAll<T>() where T : EnumerationShort =>
        typeof(T).GetFields(BindingFlags.Public |
                            BindingFlags.Static |
                            BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();
}

public class EnumerationLong : Enumeration<long>
{
    public EnumerationLong(long id, string value) : base(id, value) { }

    public static IEnumerable<T> GetAll<T>() where T : EnumerationLong =>
        typeof(T).GetFields(BindingFlags.Public |
                            BindingFlags.Static |
                            BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();
}

public class EnumerationString : Enumeration<string>
{
    public EnumerationString(string id, string value) : base(id, value) { }

    public static IEnumerable<T> GetAll<T>() where T : EnumerationString =>
        typeof(T).GetFields(BindingFlags.Public |
                            BindingFlags.Static |
                            BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();
}
