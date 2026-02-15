using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Dialysis.Contracts.Ids;

/// <summary>
/// Strongly-typed identifier for a FHIR resource (generic).
/// Use for resource types without a dedicated ID type.
/// </summary>
[DebuggerDisplay("{Value}")]
public readonly struct ResourceId : IEquatable<ResourceId>
{
    private static readonly Regex FhirIdPattern = new(@"^[A-Za-z0-9\-\.]{1,64}$", RegexOptions.Compiled);

    public string Value { get; }

    private ResourceId(string value) => Value = value;

    public static ResourceId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Resource ID cannot be null or whitespace.", nameof(value));
        if (!FhirIdPattern.IsMatch(value))
            throw new ArgumentException($"Invalid Resource ID format: {value}", nameof(value));
        return new ResourceId(value);
    }

    public static ResourceId? TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !FhirIdPattern.IsMatch(value))
            return null;
        return new ResourceId(value);
    }

    public bool Equals(ResourceId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is ResourceId other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
    public override string ToString() => Value ?? string.Empty;

    public static bool operator ==(ResourceId left, ResourceId right) => left.Equals(right);
    public static bool operator !=(ResourceId left, ResourceId right) => !left.Equals(right);

    public static implicit operator string(ResourceId id) => id.Value ?? string.Empty;
}
