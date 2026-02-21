using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;

namespace BuildingBlocks.Caching;

/// <summary>
/// Read-Through implementation using IDistributedCache.
/// On cache miss, invokes loader and stores result.
/// </summary>
public sealed class ReadThroughDistributedCache : IReadThroughCache
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;

    public ReadThroughDistributedCache(IDistributedCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<T?> GetOrLoadAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> loader,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(loader);

        byte[]? cached = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (cached is { Length: > 0 })
            return JsonSerializer.Deserialize<T>(cached, JsonOptions);

        T? value = await loader(cancellationToken).ConfigureAwait(false);
        if (value is null)
            return null;

        DistributedCacheEntryOptions opts = options ?? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = DefaultTtl };
        await _cache.SetAsync(
            key,
            JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions),
            opts,
            cancellationToken).ConfigureAwait(false);

        return value;
    }
}
