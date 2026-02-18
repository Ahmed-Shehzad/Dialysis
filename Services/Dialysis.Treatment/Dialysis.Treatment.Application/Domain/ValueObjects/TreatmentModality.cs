namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_02 – Treatment modality (how the patient receives dialysis).
/// </summary>
public readonly record struct TreatmentModality
{
    public string Value { get; }

    public TreatmentModality(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly TreatmentModality Hemodialysis = new("HD");
    public static readonly TreatmentModality Hemodiafiltration = new("HDF");
    public static readonly TreatmentModality Hemofiltration = new("HF");
    public static readonly TreatmentModality SustainedLowEfficiency = new("SLED");
    public static readonly TreatmentModality IsolatedUltrafiltration = new("IUF");
    public static readonly TreatmentModality Hemoperfusion = new("HP");

    public override string ToString() => Value;
    public static implicit operator string(TreatmentModality v) => v.Value;
    public static explicit operator TreatmentModality(string v) => new(v);
}
