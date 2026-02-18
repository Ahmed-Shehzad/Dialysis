namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_10 – Fluid flow direction (relevant for HDF substitution modes).
/// </summary>
public readonly record struct FluidFlowDirection
{
    public string Value { get; }

    public FluidFlowDirection(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly FluidFlowDirection Normal = new("NORMAL");
    public static readonly FluidFlowDirection Reverse = new("REVERSE");
    public static readonly FluidFlowDirection Bypass = new("BYPASS");

    public override string ToString() => Value;
    public static implicit operator string(FluidFlowDirection v) => v.Value;
    public static explicit operator FluidFlowDirection(string v) => new(v);
}
