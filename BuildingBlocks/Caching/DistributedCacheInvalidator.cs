using Microsoft.Extensions.Caching.Distributed;

namespace BuildingBlocks.Caching;

/// <summary>
/// Cache invalidator using IDistributedCache.RemoveAsync.
/// </summary>
public sealed class DistributedCacheInvalidator : ICacheInvalidator
{
    private readonly IDistributedCache _cache;

    public DistributedCacheInvalidator(IDistributedCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task InvalidateAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);
        foreach (string key in keys)
            if (!string.IsNullOrWhiteSpace(key))
                await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
    }
}
