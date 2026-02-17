namespace Dialysis.SharedKernel.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a dialysis session.
/// </summary>
public sealed record SessionId
{
    public string Value { get; }

    public SessionId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 64)
            throw new ArgumentException("SessionId must not exceed 64 characters.", nameof(value));
        Value = value.Trim();
    }

    public override string ToString() => Value;
    public static implicit operator string(SessionId id) => id.Value;
}
