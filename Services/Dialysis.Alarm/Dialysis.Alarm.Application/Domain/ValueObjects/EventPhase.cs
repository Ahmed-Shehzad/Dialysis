namespace Dialysis.Alarm.Application.Domain.ValueObjects;

/// <summary>
/// Alarm event phase per IEC 60601-1-8.
/// Indicates whether the alarm is starting, updating, or ending.
/// </summary>
public readonly record struct EventPhase
{
    public string Value { get; }

    public EventPhase(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly EventPhase Start = new("start");
    public static readonly EventPhase Continue = new("continue");
    public static readonly EventPhase End = new("end");

    public override string ToString() => Value;

    public static implicit operator string(EventPhase phase) => phase.Value;
    public static explicit operator EventPhase(string value) => new(value);
}
