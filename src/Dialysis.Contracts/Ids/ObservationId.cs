using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Dialysis.Contracts.Ids;

/// <summary>
/// Strongly-typed identifier for a FHIR Observation resource.
/// </summary>
[DebuggerDisplay("{Value}")]
public readonly struct ObservationId : IEquatable<ObservationId>
{
    private static readonly Regex FhirIdPattern = new(@"^[A-Za-z0-9\-\.]{1,64}$", RegexOptions.Compiled);

    public string Value { get; }

    private ObservationId(string value) => Value = value;

    public static ObservationId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Observation ID cannot be null or whitespace.", nameof(value));
        if (!FhirIdPattern.IsMatch(value))
            throw new ArgumentException($"Invalid Observation ID format: {value}", nameof(value));
        return new ObservationId(value);
    }

    public static ObservationId? TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !FhirIdPattern.IsMatch(value))
            return null;
        return new ObservationId(value);
    }

    public bool Equals(ObservationId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is ObservationId other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
    public override string ToString() => Value ?? string.Empty;

    public static bool operator ==(ObservationId left, ObservationId right) => left.Equals(right);
    public static bool operator !=(ObservationId left, ObservationId right) => !left.Equals(right);

    public static implicit operator string(ObservationId id) => id.Value ?? string.Empty;
}
