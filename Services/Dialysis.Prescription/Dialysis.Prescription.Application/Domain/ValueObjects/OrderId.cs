namespace Dialysis.Prescription.Application.Domain.ValueObjects;

/// <summary>
/// Strongly-typed prescription order identifier — from HL7 RSP^K22 ORC-2 (Placer Order Number).
/// </summary>
public readonly record struct OrderId
{
    public string Value { get; }

    public OrderId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(OrderId id) => id.Value;
    public static explicit operator OrderId(string value) => new(value);
}
