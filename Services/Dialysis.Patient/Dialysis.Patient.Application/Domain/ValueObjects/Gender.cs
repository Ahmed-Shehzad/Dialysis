namespace Dialysis.Patient.Application.Domain.ValueObjects;

/// <summary>
/// HL7 administrative gender (PID-8). Uses HL7 Table 0001 values.
/// </summary>
public readonly record struct Gender
{
    public string Value { get; }

    public Gender(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly Gender Male = new("M");
    public static readonly Gender Female = new("F");
    public static readonly Gender Other = new("O");
    public static readonly Gender Unknown = new("U");

    public override string ToString() => Value;

    public static implicit operator string(Gender gender) => gender.Value;
    public static explicit operator Gender(string value) => new(value);
}
