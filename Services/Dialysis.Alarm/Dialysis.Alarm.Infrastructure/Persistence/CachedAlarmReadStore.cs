using BuildingBlocks.Caching;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Abstractions;

using Microsoft.Extensions.Caching.Distributed;

namespace Dialysis.Alarm.Infrastructure.Persistence;

/// <summary>
/// Read-Through cache for alarm lookup by Id. Uses tenant-scoped keys (C5).
/// </summary>
public sealed class CachedAlarmReadStore : IAlarmReadStore
{
    private const string KeyPrefix = "alarm";
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    private readonly IAlarmReadStore _inner;
    private readonly IReadThroughCache _readThrough;

    public CachedAlarmReadStore(IAlarmReadStore inner, IReadThroughCache readThrough)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _readThrough = readThrough ?? throw new ArgumentNullException(nameof(readThrough));
    }

    public Task<AlarmReadDto?> GetByIdAsync(string tenantId, string alarmId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(alarmId)) return Task.FromResult<AlarmReadDto?>(null);
        string key = $"{tenantId}:{KeyPrefix}:{alarmId}";
        return _readThrough.GetOrLoadAsync(
            key,
            ct => _inner.GetByIdAsync(tenantId, alarmId, ct),
            CacheOptions,
            cancellationToken);
    }

    public Task<IReadOnlyList<AlarmReadDto>> GetAlarmsAsync(string tenantId, DeviceId? deviceId, string? sessionId, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CancellationToken cancellationToken = default)
        => _inner.GetAlarmsAsync(tenantId, deviceId, sessionId, fromUtc, toUtc, cancellationToken);
}
