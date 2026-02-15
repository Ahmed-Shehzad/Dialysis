using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Dialysis.Contracts.Ids;

[DebuggerDisplay("{Value}")]
public readonly struct PatientId : IEquatable<PatientId>
{
    private static readonly Regex FhirIdPattern = new(@"^[A-Za-z0-9\-\.]{1,64}$", RegexOptions.Compiled);

    public string Value { get; }

    private PatientId(string value) => Value = value;

    public static PatientId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Patient ID cannot be null or whitespace.", nameof(value));
        if (!FhirIdPattern.IsMatch(value))
            throw new ArgumentException($"Invalid Patient ID format: {value}", nameof(value));
        return new PatientId(value);
    }

    public static PatientId? TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !FhirIdPattern.IsMatch(value))
            return null;
        return new PatientId(value);
    }

    public bool Equals(PatientId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is PatientId other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
    public override string ToString() => Value ?? string.Empty;

    public static bool operator ==(PatientId left, PatientId right) => left.Equals(right);
    public static bool operator !=(PatientId left, PatientId right) => !left.Equals(right);

    public static implicit operator string(PatientId id) => id.Value ?? string.Empty;
}
