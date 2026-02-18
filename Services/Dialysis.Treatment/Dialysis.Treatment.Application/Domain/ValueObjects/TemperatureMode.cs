namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_09 – Temperature control mode.
/// </summary>
public readonly record struct TemperatureMode
{
    public string Value { get; }

    public TemperatureMode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly TemperatureMode Constant = new("CONST");
    public static readonly TemperatureMode Auto = new("AUTO");
    public static readonly TemperatureMode Profile = new("PRO");
    public static readonly TemperatureMode None = new("NONE");

    public override string ToString() => Value;
    public static implicit operator string(TemperatureMode v) => v.Value;
    public static explicit operator TemperatureMode(string v) => new(v);
}
