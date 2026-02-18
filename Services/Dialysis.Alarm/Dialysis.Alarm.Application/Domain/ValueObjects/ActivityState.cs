namespace Dialysis.Alarm.Application.Domain.ValueObjects;

/// <summary>
/// Alarm system activity state — whether the alarm source is enabled or disabled.
/// </summary>
public readonly record struct ActivityState
{
    public string Value { get; }

    public ActivityState(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly ActivityState Enabled = new("enabled");
    public static readonly ActivityState AudioPaused = new("audio-paused");
    public static readonly ActivityState AudioOff = new("audio-off");
    public static readonly ActivityState AlarmPaused = new("alarm-paused");
    public static readonly ActivityState AlarmOff = new("alarm-off");
    public static readonly ActivityState AlertAcknowledged = new("alert-acknowledged");

    public override string ToString() => Value;

    public static implicit operator string(ActivityState state) => state.Value;
    public static explicit operator ActivityState(string value) => new(value);
}
