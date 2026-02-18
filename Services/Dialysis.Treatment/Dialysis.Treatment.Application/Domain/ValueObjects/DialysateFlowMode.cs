namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_06 – Dialysate flow mode.
/// </summary>
public readonly record struct DialysateFlowMode
{
    public string Value { get; }

    public DialysateFlowMode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly DialysateFlowMode Constant = new("CONST");
    public static readonly DialysateFlowMode Auto = new("AUTO");
    public static readonly DialysateFlowMode Profile = new("PRO");
    public static readonly DialysateFlowMode Standby = new("STBY");
    public static readonly DialysateFlowMode None = new("NONE");

    public override string ToString() => Value;
    public static implicit operator string(DialysateFlowMode v) => v.Value;
    public static explicit operator DialysateFlowMode(string v) => new(v);
}
