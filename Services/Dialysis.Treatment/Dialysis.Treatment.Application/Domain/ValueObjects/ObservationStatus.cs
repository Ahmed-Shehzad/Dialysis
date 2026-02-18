namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// OBX-11 observation result status.
/// </summary>
public readonly record struct ObservationStatus
{
    public string Value { get; }

    public ObservationStatus(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly ObservationStatus Final = new("F");
    public static readonly ObservationStatus Corrected = new("C");
    public static readonly ObservationStatus Preliminary = new("P");
    public static readonly ObservationStatus ResultNotAvailable = new("X");

    public override string ToString() => Value;
    public static implicit operator string(ObservationStatus v) => v.Value;
    public static explicit operator ObservationStatus(string v) => new(v);
}
