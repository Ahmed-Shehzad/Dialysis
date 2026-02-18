namespace BuildingBlocks.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a dialysis device.
/// </summary>
public readonly record struct DeviceId
{
    public string Value { get; }

    public DeviceId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(DeviceId id) => id.Value;
    public static explicit operator DeviceId(string value) => new(value);
}
