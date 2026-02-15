using System.Text.Json;
using Dialysis.Alerting.Features.ProcessAlerts;
using Dialysis.Tenancy;
using Microsoft.Extensions.Caching.Distributed;

namespace Dialysis.Alerting.Services;

public interface IAlertCacheService
{
    Task<IReadOnlyList<AlertSummaryDto>?> GetListAsync(AlertStatusFilter? status, CancellationToken cancellationToken = default);
    Task SetListAsync(AlertStatusFilter? status, IReadOnlyList<AlertSummaryDto> alerts, CancellationToken cancellationToken = default);
    Task InvalidateListAsync(CancellationToken cancellationToken = default);
}

public sealed class AlertCacheService : IAlertCacheService
{
    private const string CacheKeyPrefix = "alerts:list";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IDistributedCache _cache;
    private readonly ITenantContext _tenantContext;

    public AlertCacheService(IDistributedCache cache, ITenantContext tenantContext)
    {
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<AlertSummaryDto>?> GetListAsync(AlertStatusFilter? status, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(status);
        var bytes = await _cache.GetAsync(key, cancellationToken);
        if (bytes is null)
        {
            return null;
        }

        var list = JsonSerializer.Deserialize<List<AlertSummaryDto>>(bytes, JsonOptions);
        return list?.AsReadOnly();
    }

    public async Task SetListAsync(AlertStatusFilter? status, IReadOnlyList<AlertSummaryDto> alerts, CancellationToken cancellationToken = default)
    {
        var key = GetCacheKey(status);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(alerts.ToList(), JsonOptions);
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl };
        await _cache.SetAsync(key, bytes, options, cancellationToken);
    }

    public async Task InvalidateListAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new AlertStatusFilter?[] { null, AlertStatusFilter.Active, AlertStatusFilter.Acknowledged };
        foreach (var s in statuses)
        {
            await _cache.RemoveAsync(GetCacheKey(s), cancellationToken);
        }
    }

    private string GetCacheKey(AlertStatusFilter? status)
    {
        var tenantId = _tenantContext.TenantId ?? "default";
        var baseKey = $"{CacheKeyPrefix}:{tenantId}";
        return status.HasValue ? $"{baseKey}:{status}" : baseKey;
    }
}
