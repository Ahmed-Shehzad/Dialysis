namespace Dialysis.Treatment.Application.Domain.ValueObjects;

/// <summary>
/// Vascular access type for dialysis (AVF, AVG, CVC).
/// </summary>
public readonly record struct AccessType
{
    public string Value { get; }

    public AccessType(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public static readonly AccessType Avf = new("AVF");
    public static readonly AccessType Avg = new("AVG");
    public static readonly AccessType Cvc = new("CVC");

    public override string ToString() => Value;

    public static implicit operator string(AccessType t) => t.Value;
    public static explicit operator AccessType(string value) => new(value);
}
