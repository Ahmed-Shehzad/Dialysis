namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// _TBL_17 – IHE PCD event phase for observations.
/// Maps to OBR-12 in HL7 messages.
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
    public static implicit operator string(EventPhase v) => v.Value;
    public static explicit operator EventPhase(string v) => new(v);
}
