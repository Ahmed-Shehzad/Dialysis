using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace Dialysis.Module.Bff;

/// <summary>
/// Server-side <see cref="ITicketStore"/> backed by <see cref="IDistributedCache"/> (Valkey in the
/// deployment stack, an in-memory distributed cache in dev). With <c>SaveTokens = true</c> the OIDC
/// handler would otherwise pack the full access/id/refresh token bundle into the auth cookie, which
/// the cookie middleware then chunks across <c>.Cookie</c>, <c>…C1</c>, <c>…C2</c>, … On the single
/// shared gateway origin where seven per-context BFF cookies coexist, that accumulation overflowed
/// Kestrel's 32&#160;KB request-header limit and produced HTTP&#160;431 on plain SPA loads.
/// Storing the ticket server-side leaves only a short session key in the browser cookie, so the
/// Cookie header stays small regardless of how many tokens (or upstream-IdP claims) the session holds.
/// </summary>
public sealed class DistributedCacheTicketStore : ITicketStore
{
    // Cookie sessions without an explicit ExpiresUtc (non-persistent) still need a server-side TTL so
    // abandoned tickets don't accumulate in Valkey forever. Generous enough to outlive a working day.
    private static readonly TimeSpan FallbackTtl = TimeSpan.FromHours(8);

    private readonly IDistributedCache _cache;
    private readonly string _keyPrefix;

    /// <summary>Creates the store, namespacing every key under <paramref name="keyPrefix"/> so BFFs
    /// sharing one Valkey instance never read each other's tickets.</summary>
    public DistributedCacheTicketStore(IDistributedCache cache, string keyPrefix)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _keyPrefix = keyPrefix ?? "";
    }

    /// <inheritdoc />
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        var key = _keyPrefix + Guid.NewGuid().ToString("N");
        await RenewAsync(key, ticket).ConfigureAwait(false);
        return key;
    }

    /// <inheritdoc />
    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        var options = new DistributedCacheEntryOptions();
        var expiresUtc = ticket.Properties.ExpiresUtc;
        if (expiresUtc.HasValue)
            options.SetAbsoluteExpiration(expiresUtc.Value);
        else
            options.SetSlidingExpiration(FallbackTtl);

        var bytes = TicketSerializer.Default.Serialize(ticket);
        await _cache.SetAsync(key, bytes, options).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var bytes = await _cache.GetAsync(key).ConfigureAwait(false);
        return bytes is null ? null : TicketSerializer.Default.Deserialize(bytes);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key) => _cache.RemoveAsync(key);
}
