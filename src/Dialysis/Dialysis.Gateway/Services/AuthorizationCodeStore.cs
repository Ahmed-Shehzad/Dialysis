namespace Dialysis.Gateway.Services;

/// <summary>
/// In-memory store for OAuth2 authorization codes. For production, use Redis or similar.
/// </summary>
public interface IAuthorizationCodeStore
{
    void Store(string code, StoredAuthorizationCode data, TimeSpan ttl);
    StoredAuthorizationCode? Consume(string code);
}

public sealed record StoredAuthorizationCode(string ClientId, string RedirectUri, string Scope, string? State, string? TenantId);

public sealed class InMemoryAuthorizationCodeStore : IAuthorizationCodeStore
{
    private readonly Dictionary<string, (StoredAuthorizationCode Data, DateTimeOffset Expires)> _codes = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public void Store(string code, StoredAuthorizationCode data, TimeSpan ttl)
    {
        _lock.EnterWriteLock();
        try
        {
            _codes[code] = (data, DateTimeOffset.UtcNow.Add(ttl));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public StoredAuthorizationCode? Consume(string code)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_codes.TryGetValue(code, out var entry))
                return null;
            if (DateTimeOffset.UtcNow > entry.Expires)
            {
                _codes.Remove(code);
                return null;
            }
            _codes.Remove(code);
            return entry.Data;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
