namespace Dialysis.SharedKernel.ValueObjects;

/// <summary>
/// LOINC code for clinical observations. Avoids primitive obsession.
/// </summary>
public sealed record LoincCode
{
    public string Value { get; }

    public LoincCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 32)
            throw new ArgumentException("LoincCode must not exceed 32 characters.", nameof(value));
        Value = value.Trim();
    }

    public static readonly LoincCode BloodPressure = new("85354-9");
    public static readonly LoincCode HeartRate = new("8867-4");
    public static readonly LoincCode BodyWeight = new("29463-7");

    public override string ToString() => Value;
    public static implicit operator string(LoincCode code) => code.Value;
}
