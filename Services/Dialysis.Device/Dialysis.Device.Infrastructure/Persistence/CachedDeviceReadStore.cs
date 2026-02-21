using BuildingBlocks.Caching;

using Dialysis.Device.Application.Abstractions;

using Microsoft.Extensions.Caching.Distributed;

namespace Dialysis.Device.Infrastructure.Persistence;

/// <summary>
/// Read-Through cache for device lookup by Id and DeviceEui64. Uses tenant-scoped keys (C5).
/// </summary>
public sealed class CachedDeviceReadStore : IDeviceReadStore
{
    private const string KeyPrefix = "device";
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    private readonly IDeviceReadStore _inner;
    private readonly IReadThroughCache _readThrough;

    public CachedDeviceReadStore(IDeviceReadStore inner, IReadThroughCache readThrough)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _readThrough = readThrough ?? throw new ArgumentNullException(nameof(readThrough));
    }

    public Task<DeviceReadDto?> GetByIdAsync(string tenantId, string deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return Task.FromResult<DeviceReadDto?>(null);
        string key = $"{tenantId}:{KeyPrefix}:id:{deviceId}";
        return _readThrough.GetOrLoadAsync(
            key,
            ct => _inner.GetByIdAsync(tenantId, deviceId, ct),
            CacheOptions,
            cancellationToken);
    }

    public Task<DeviceReadDto?> GetByDeviceEui64Async(string tenantId, string deviceEui64, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceEui64)) return Task.FromResult<DeviceReadDto?>(null);
        string key = $"{tenantId}:{KeyPrefix}:eui64:{deviceEui64}";
        return _readThrough.GetOrLoadAsync(
            key,
            ct => _inner.GetByDeviceEui64Async(tenantId, deviceEui64, ct),
            CacheOptions,
            cancellationToken);
    }

    public Task<IReadOnlyList<DeviceReadDto>> GetAllForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        => _inner.GetAllForTenantAsync(tenantId, cancellationToken);
}
