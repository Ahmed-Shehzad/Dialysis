namespace BuildingBlocks.Caching;

/// <summary>
/// No-op Read-Through cache when Redis is not configured. Always invokes loader.
/// </summary>
public sealed class NullReadThroughCache : IReadThroughCache
{
    public Task<T?> GetOrLoadAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> loader,
        Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(loader);
        return loader(cancellationToken);
    }
}
