namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_03 – Anticoagulation administration mode.
/// </summary>
public readonly record struct AnticoagulationMode
{
    public string Value { get; }

    public AnticoagulationMode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly AnticoagulationMode Bolus = new("BOL");
    public static readonly AnticoagulationMode Continuous = new("CON");
    public static readonly AnticoagulationMode BolusContinuous = new("BOLCON");
    public static readonly AnticoagulationMode Profile = new("PRO");
    public static readonly AnticoagulationMode BolusProfile = new("BOLPRO");
    public static readonly AnticoagulationMode None = new("NONE");

    public override string ToString() => Value;
    public static implicit operator string(AnticoagulationMode v) => v.Value;
    public static explicit operator AnticoagulationMode(string v) => new(v);
}
