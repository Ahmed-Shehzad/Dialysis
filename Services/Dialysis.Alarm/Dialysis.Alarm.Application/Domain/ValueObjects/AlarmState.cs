namespace Dialysis.Alarm.Application.Domain.ValueObjects;

/// <summary>
/// Alarm condition state per IEC 60601-1-8 / IEEE 11073.
/// Maps to OBX-5 alarm state in PCD-04 ORU^R40.
/// </summary>
public readonly record struct AlarmState
{
    public string Value { get; }

    public AlarmState(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly AlarmState Off = new("off");
    public static readonly AlarmState Inactive = new("inactive");
    public static readonly AlarmState Active = new("active");
    public static readonly AlarmState Latched = new("latched");
    public static readonly AlarmState Acknowledged = new("acknowledged");
    public static readonly AlarmState Cleared = new("cleared");

    public override string ToString() => Value;

    public static implicit operator string(AlarmState state) => state.Value;
    public static explicit operator AlarmState(string value) => new(value);
}
