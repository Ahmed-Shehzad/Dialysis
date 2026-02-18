namespace Dialysis.Prescription.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_17 – Prescription profile type for pumpable parameters.
/// </summary>
public readonly record struct ProfileType
{
    public string Value { get; }

    public ProfileType(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly ProfileType Constant = new("CONSTANT");
    public static readonly ProfileType Linear = new("LINEAR");
    public static readonly ProfileType Exponential = new("EXPONENTIAL");
    public static readonly ProfileType Step = new("STEP");
    public static readonly ProfileType Vendor = new("VENDOR");

    public override string ToString() => Value;
    public static implicit operator string(ProfileType v) => v.Value;
    public static explicit operator ProfileType(string v) => new(v);
}
