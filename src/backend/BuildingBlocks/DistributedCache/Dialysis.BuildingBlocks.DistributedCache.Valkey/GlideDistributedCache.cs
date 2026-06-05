using Microsoft.Extensions.Caching.Distributed;
using Valkey.Glide;

namespace Dialysis.BuildingBlocks.DistributedCache.Valkey;

/// <summary>
/// <see cref="IDistributedCache"/> over Valkey GLIDE. Replaces the StackExchange.Redis-backed
/// <c>RedisCache</c> (which can't consume a GLIDE connection — the Microsoft package binds the
/// concrete StackExchange.Redis assembly types). Keys are prefixed with the module instance name
/// to mirror the previous behaviour so multiple modules sharing one Valkey cluster don't collide.
///
/// Absolute expiration (the only kind used in this codebase) maps to a Valkey TTL at write time.
/// Sliding expiration is supported by stashing the window in a companion key and re-applying the
/// TTL on each read (GLIDE has no server-side sliding primitive, same as StackExchange.Redis).
///
/// GLIDE's IDatabase is async-only, so the synchronous interface members bridge to the async ones.
/// That bridge is safe here: ASP.NET Core has no SynchronizationContext and GLIDE completes its
/// tasks off-context, so there is no sync-over-async deadlock. Our own call sites use the async
/// members; the sync members exist only to satisfy the interface.
/// </summary>
internal sealed class GlideDistributedCache : IDistributedCache
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly string _prefix;

    public GlideDistributedCache(IConnectionMultiplexer multiplexer, string instanceName)
    {
        ArgumentNullException.ThrowIfNull(multiplexer);
        _multiplexer = multiplexer;
        _prefix = string.IsNullOrEmpty(instanceName) ? string.Empty : instanceName + ":";
    }

    private IDatabase Db => _multiplexer.GetDatabase();
    private string DataKey(string key) => _prefix + key;
    private string SlidingKey(string key) => _prefix + key + ":sld";

#pragma warning disable VSTHRD002 // Sync-over-async bridge for the sync interface members; safe (no SynchronizationContext, GLIDE completes off-context).
    public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();

    public void Refresh(string key) => RefreshAsync(key).GetAwaiter().GetResult();

    public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        SetAsync(key, value, options).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var value = await Db.StringGetAsync(DataKey(key)).ConfigureAwait(false);
        if (value.IsNull)
            return null;
        await RefreshSlidingAsync(key).ConfigureAwait(false);
        return (byte[]?)value;
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        return RefreshSlidingAsync(key);
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        await Db.KeyDeleteAsync(DataKey(key)).ConfigureAwait(false);
        await Db.KeyDeleteAsync(SlidingKey(key)).ConfigureAwait(false);
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        var (ttl, sliding) = Plan(key, value, options);
        await Db.StringSetAsync(DataKey(key), value, ttl).ConfigureAwait(false);
        if (sliding is { } window)
            await Db.StringSetAsync(SlidingKey(key), (long)window.TotalSeconds, ttl).ConfigureAwait(false);
        else
            await Db.KeyDeleteAsync(SlidingKey(key)).ConfigureAwait(false);
    }

    /// <summary>Validates inputs and computes the effective TTL + sliding window for a write.</summary>
    private static (TimeSpan? Ttl, TimeSpan? Sliding) Plan(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);

        var now = DateTimeOffset.UtcNow;
        TimeSpan? absolute = null;
        if (options.AbsoluteExpirationRelativeToNow is { } relative)
            absolute = relative;
        else if (options.AbsoluteExpiration is { } at)
            absolute = at > now ? at - now : TimeSpan.Zero;

        var sliding = options.SlidingExpiration;
        var ttl = (absolute, sliding) switch
        {
            (null, null) => (TimeSpan?)null,
            ({ } a, null) => a,
            (null, { } s) => s,
            ({ } a, { } s) => a < s ? a : s,
        };
        return (ttl, sliding);
    }

    private async Task RefreshSlidingAsync(string key)
    {
        var sliding = await Db.StringGetAsync(SlidingKey(key)).ConfigureAwait(false);
        if (sliding.IsNull)
            return;
        var window = TimeSpan.FromSeconds((long)sliding);
        await Db.KeyExpireAsync(DataKey(key), window).ConfigureAwait(false);
        await Db.KeyExpireAsync(SlidingKey(key), window).ConfigureAwait(false);
    }
}
