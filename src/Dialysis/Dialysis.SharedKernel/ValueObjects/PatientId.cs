namespace Dialysis.SharedKernel.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a patient. Avoids primitive obsession.
/// </summary>
public sealed record PatientId
{
    public string Value { get; }

    public PatientId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 64)
            throw new ArgumentException("PatientId must not exceed 64 characters.", nameof(value));
        Value = value.Trim();
    }

    public override string ToString() => Value;
    public static implicit operator string(PatientId id) => id.Value;
}
