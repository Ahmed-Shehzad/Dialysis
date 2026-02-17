namespace Dialysis.SharedKernel.ValueObjects;

/// <summary>
/// Strongly-typed identifier for an observation. Avoids primitive obsession.
/// </summary>
public sealed record ObservationId
{
    public string Value { get; }

    public ObservationId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 64)
            throw new ArgumentException("ObservationId must not exceed 64 characters.", nameof(value));
        Value = value.Trim();
    }

    public override string ToString() => Value;
    public static implicit operator string(ObservationId id) => id.Value;
}
