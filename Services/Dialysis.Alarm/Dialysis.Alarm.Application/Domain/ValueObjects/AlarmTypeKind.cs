namespace Dialysis.Alarm.Application.Domain.ValueObjects;

/// <summary>
/// PCD-04 alarm event type from OBX #1 (OBX-3).
/// </summary>
public readonly record struct AlarmTypeKind
{
    public string Value { get; }

    public AlarmTypeKind(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly AlarmTypeKind BelowLimit = new("MDC_EVT_LO");
    public static readonly AlarmTypeKind AboveLimit = new("MDC_EVT_HI");
    public static readonly AlarmTypeKind NonNumeric = new("MDC_EVT_ALARM");

    public override string ToString() => Value;
    public static implicit operator string(AlarmTypeKind v) => v.Value;
    public static explicit operator AlarmTypeKind(string v) => new(v);
}
