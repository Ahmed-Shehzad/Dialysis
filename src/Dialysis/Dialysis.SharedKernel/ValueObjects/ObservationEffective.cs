namespace Dialysis.SharedKernel.ValueObjects;

/// <summary>
/// Effective date-time for an observation. Avoids primitive obsession.
/// </summary>
public sealed record ObservationEffective
{
    public DateTimeOffset Value { get; }

    public ObservationEffective(DateTimeOffset value)
    {
        Value = value;
    }

    public static ObservationEffective UtcNow => new(DateTimeOffset.UtcNow);

    public override string ToString() => Value.ToString("O");
}
