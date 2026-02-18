namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_08 – Replacement fluid infusion location.
/// </summary>
public readonly record struct ReplacementFluidLocation
{
    public string Value { get; }

    public ReplacementFluidLocation(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly ReplacementFluidLocation PreDilution = new("PRE");
    public static readonly ReplacementFluidLocation PostDilution = new("POST");
    public static readonly ReplacementFluidLocation PrePostMix = new("MIX");
    public static readonly ReplacementFluidLocation None = new("NONE");

    public override string ToString() => Value;
    public static implicit operator string(ReplacementFluidLocation v) => v.Value;
    public static explicit operator ReplacementFluidLocation(string v) => new(v);
}
