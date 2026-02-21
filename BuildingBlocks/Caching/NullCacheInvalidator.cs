namespace BuildingBlocks.Caching;

/// <summary>
/// No-op cache invalidator when Redis is not configured.
/// </summary>
public sealed class NullCacheInvalidator : ICacheInvalidator
{
    public Task InvalidateAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task InvalidateAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
