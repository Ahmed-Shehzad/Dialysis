namespace Dialysis.Alarm.Application.Domain.ValueObjects;

/// <summary>
/// PCD-04 alarm priority from OBX-8 interpretation codes (PH=high, PM=medium, PL=low).
/// </summary>
public readonly record struct AlarmPriority
{
    public string Value { get; }

    public AlarmPriority(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly AlarmPriority High = new("PH");
    public static readonly AlarmPriority Medium = new("PM");
    public static readonly AlarmPriority Low = new("PL");
    public static readonly AlarmPriority Informational = new("PI");
    public static readonly AlarmPriority NoAlarm = new("PN");
    public static readonly AlarmPriority Unknown = new("PU");

    public override string ToString() => Value;
    public static implicit operator string(AlarmPriority v) => v.Value;
    public static explicit operator AlarmPriority(string v) => new(v);
}
