namespace Dialysis.Device.Infrastructure.ReadModels;

/// <summary>
/// Read-only projection of Device for query operations. Maps to the Devices table.
/// </summary>
public sealed class DeviceReadModel
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string DeviceEui64 { get; init; } = string.Empty;
    public string? Manufacturer { get; init; }
    public string? Model { get; init; }
    public string? Serial { get; init; }
    public string? Udi { get; init; }
}
