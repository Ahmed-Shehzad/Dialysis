namespace Dialysis.Device.Application.Abstractions;

/// <summary>
/// Read-only store for Device queries. Used by query handlers instead of the write repository.
/// </summary>
public interface IDeviceReadStore
{
    Task<DeviceReadDto?> GetByIdAsync(string tenantId, string deviceId, CancellationToken cancellationToken = default);
    Task<DeviceReadDto?> GetByDeviceEui64Async(string tenantId, string deviceEui64, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceReadDto>> GetAllForTenantAsync(string tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for device query results.
/// </summary>
public sealed record DeviceReadDto(
    string Id,
    string DeviceEui64,
    string? Manufacturer,
    string? Model,
    string? Serial,
    string? Udi);
