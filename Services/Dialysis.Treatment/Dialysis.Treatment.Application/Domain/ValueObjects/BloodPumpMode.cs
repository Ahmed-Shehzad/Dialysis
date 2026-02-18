namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_05 – Blood pump operating mode.
/// </summary>
public readonly record struct BloodPumpMode
{
    public string Value { get; }

    public BloodPumpMode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly BloodPumpMode DoubleNeedle = new("2N");
    public static readonly BloodPumpMode SingleNeedleSinglePump = new("1N1P");
    public static readonly BloodPumpMode SingleNeedleDoublePump = new("1N2P");

    public override string ToString() => Value;
    public static implicit operator string(BloodPumpMode v) => v.Value;
    public static explicit operator BloodPumpMode(string v) => new(v);
}
