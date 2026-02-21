namespace BuildingBlocks.Caching;

/// <summary>
/// Invalidates cache entries on write. Part of Write-Through strategy: when data is written to DB,
/// remove affected cache keys so the next read loads fresh data.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>
    /// Removes the cache entry for the given key.
    /// </summary>
    Task InvalidateAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple cache entries.
    /// </summary>
    Task InvalidateAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
}
