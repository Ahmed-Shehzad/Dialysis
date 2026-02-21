using Microsoft.Extensions.Caching.Distributed;

namespace BuildingBlocks.Caching;

/// <summary>
/// Read-Through cache: on miss, loads from data source and populates cache automatically.
/// Simplifies app logic—callers just "get"; no explicit cache-aside.
/// </summary>
public interface IReadThroughCache
{
    /// <summary>
    /// Gets the value from cache, or loads it via <paramref name="loader"/> on miss and stores it.
    /// </summary>
    /// <param name="key">Cache key (tenant-scoped, e.g. {tenantId}:{entity}:{id}).</param>
    /// <param name="loader">Called on cache miss to load from data source.</param>
    /// <param name="options">Cache entry options (TTL). If null, uses default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The value, or null if loader returns null.</returns>
    Task<T?> GetOrLoadAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> loader,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default) where T : class;
}
