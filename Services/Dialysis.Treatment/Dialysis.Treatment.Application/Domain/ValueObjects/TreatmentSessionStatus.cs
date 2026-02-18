namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// Status of a dialysis treatment session.
/// </summary>
public readonly record struct TreatmentSessionStatus
{
    public string Value { get; }

    public TreatmentSessionStatus(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly TreatmentSessionStatus Active = new("Active");
    public static readonly TreatmentSessionStatus Completed = new("Completed");

    public override string ToString() => Value;

    public static implicit operator string(TreatmentSessionStatus status) => status.Value;
    public static explicit operator TreatmentSessionStatus(string value) => new(value);
}
