namespace BuildingBlocks.ValueObjects;

/// <summary>
/// Strongly-typed Medical Record Number (MRN) — unique patient identifier within a facility.
/// Aligns with HL7 PID-3 (Patient Identifier List).
/// </summary>
public readonly record struct MedicalRecordNumber
{
    public string Value { get; }

    public MedicalRecordNumber(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(MedicalRecordNumber mrn) => mrn.Value;
    public static explicit operator MedicalRecordNumber(string value) => new(value);
}
