namespace Dialysis.Device.Application.Domain.ValueObjects;

/// <summary>
/// Strongly-typed device EUI-64 identifier — from HL7 MSH-3 (e.g. MACH^EUI64^EUI-64).
/// </summary>
public readonly record struct DeviceEui64
{
    public string Value { get; }

    public DeviceEui64(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public override string ToString() => Value;

    public static implicit operator string(DeviceEui64 id) => id.Value;
    public static explicit operator DeviceEui64(string value) => new(value);
}
