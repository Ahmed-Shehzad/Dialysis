using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Dialysis.Contracts.Ids;

/// <summary>
/// Strongly-typed identifier for a FHIR Encounter resource.
/// </summary>
[DebuggerDisplay("{Value}")]
public readonly struct EncounterId : IEquatable<EncounterId>
{
    private static readonly Regex FhirIdPattern = new(@"^[A-Za-z0-9\-\.]{1,64}$", RegexOptions.Compiled);

    public string Value { get; }

    private EncounterId(string value) => Value = value;

    public static EncounterId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Encounter ID cannot be null or whitespace.", nameof(value));
        if (!FhirIdPattern.IsMatch(value))
            throw new ArgumentException($"Invalid Encounter ID format: {value}", nameof(value));
        return new EncounterId(value);
    }

    public static EncounterId? TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !FhirIdPattern.IsMatch(value))
            return null;
        return new EncounterId(value);
    }

    public bool Equals(EncounterId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is EncounterId other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
    public override string ToString() => Value ?? string.Empty;

    public static bool operator ==(EncounterId left, EncounterId right) => left.Equals(right);
    public static bool operator !=(EncounterId left, EncounterId right) => !left.Equals(right);

    public static implicit operator string(EncounterId id) => id.Value ?? string.Empty;
}
