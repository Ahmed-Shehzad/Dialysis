namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_07 – Replacement fluid delivery mode.
/// </summary>
public readonly record struct ReplacementFluidFlowMode
{
    public string Value { get; }

    public ReplacementFluidFlowMode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly ReplacementFluidFlowMode Constant = new("CONST");
    public static readonly ReplacementFluidFlowMode Profile = new("PRO");
    public static readonly ReplacementFluidFlowMode None = new("NONE");
    public static readonly ReplacementFluidFlowMode Bolus = new("BOLUS");
    public static readonly ReplacementFluidFlowMode Auto = new("AUTO");

    public override string ToString() => Value;
    public static implicit operator string(ReplacementFluidFlowMode v) => v.Value;
    public static explicit operator ReplacementFluidFlowMode(string v) => new(v);
}
