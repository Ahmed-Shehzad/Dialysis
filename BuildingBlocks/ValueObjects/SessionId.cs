namespace BuildingBlocks.ValueObjects;

/// <summary>
/// Strongly-typed treatment session identifier — typically derived from HL7 OBR-3 (Filler Order Number).
/// </summary>
public readonly record struct SessionId
{
    public string Value { get; }

    public SessionId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(SessionId id) => id.Value;
    public static explicit operator SessionId(string value) => new(value);
}
