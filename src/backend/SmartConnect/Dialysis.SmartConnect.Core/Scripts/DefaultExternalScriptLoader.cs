using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Scripts;

/// <summary>
/// Loads JavaScript bodies referenced by <c>file://</c> or <c>http(s)://</c> URIs.
/// Caches successful loads in-process keyed by (canonical URI, TTL). Enforces
/// <see cref="ExternalScriptOptions"/> scheme/root/host allow-lists and a max body size.
/// </summary>
public sealed class DefaultExternalScriptLoader : IExternalScriptLoader
{
    private readonly IOptionsMonitor<ExternalScriptOptions> _options;
    private readonly IHttpClientFactory _httpClients;
    private readonly TimeProvider _time;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);

    public DefaultExternalScriptLoader(IOptionsMonitor<ExternalScriptOptions> options, IHttpClientFactory httpClients, TimeProvider time)
    {
        _options = options;
        _httpClients = httpClients;
        _time = time;
    }

    public async Task<string> LoadAsync(Uri uri, TimeSpan? cacheTtl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(uri);

        var opts = _options.CurrentValue;
        var effectiveTtl = cacheTtl ?? opts.DefaultCacheTtl;
        var nowUtc = _time.GetUtcNow();
        var key = uri.AbsoluteUri;

        if (effectiveTtl > TimeSpan.Zero && _cache.TryGetValue(key, out var hit) && hit.ExpiresAtUtc > nowUtc)
        {
            return hit.Body;
        }

        string body = uri.Scheme switch
        {
            "file" => await LoadFileAsync(uri, opts, cancellationToken).ConfigureAwait(false),
            "http" or "https" => await LoadHttpAsync(uri, opts, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"External script URI scheme '{uri.Scheme}' is not supported."),
        };

        if (effectiveTtl > TimeSpan.Zero)
        {
            _cache[key] = new CacheEntry(body, nowUtc + effectiveTtl);
        }
        return body;
    }

    private async Task<string> LoadFileAsync(Uri uri, ExternalScriptOptions opts, CancellationToken cancellationToken)
    {
        if (opts.AllowedFileRoots is null || opts.AllowedFileRoots.Count == 0)
        {
            throw new InvalidOperationException("External script file:// access is disabled. Configure ExternalScriptOptions.AllowedFileRoots.");
        }

        var fullPath = Path.GetFullPath(uri.LocalPath);
        var allowed = opts.AllowedFileRoots
            .Select(static r => Path.GetFullPath(r).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            .Any(root => IsWithin(fullPath, root));
        if (!allowed)
        {
            throw new InvalidOperationException($"External script path '{fullPath}' is outside the configured AllowedFileRoots.");
        }

        var info = new FileInfo(fullPath);
        if (!info.Exists)
        {
            throw new FileNotFoundException($"External script not found: {fullPath}");
        }
        if (info.Length > opts.MaxScriptBytes)
        {
            throw new InvalidOperationException($"External script '{fullPath}' exceeds MaxScriptBytes ({opts.MaxScriptBytes}).");
        }
        return await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> LoadHttpAsync(Uri uri, ExternalScriptOptions opts, CancellationToken cancellationToken)
    {
        if (opts.AllowedHttpHosts is null || opts.AllowedHttpHosts.Count == 0)
        {
            throw new InvalidOperationException("External script http(s):// access is disabled. Configure ExternalScriptOptions.AllowedHttpHosts.");
        }
        if (!opts.AllowedHttpHosts.Any(h => string.Equals(h, uri.Host, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"External script host '{uri.Host}' is not in AllowedHttpHosts.");
        }

        var client = _httpClients.CreateClient("smartconnect-outbound");
        using var response = await client.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is { } len && len > opts.MaxScriptBytes)
        {
            throw new InvalidOperationException($"External script at '{uri}' reports {len} bytes; exceeds MaxScriptBytes ({opts.MaxScriptBytes}).");
        }
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var ms = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (ms.Length + read > opts.MaxScriptBytes)
            {
                throw new InvalidOperationException($"External script at '{uri}' exceeded MaxScriptBytes ({opts.MaxScriptBytes}) during read.");
            }
            await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
        return System.Text.Encoding.UTF8.GetString(ms.ToArray());
    }

    private static bool IsWithin(string fullPath, string root)
    {
        var rel = Path.GetRelativePath(root, fullPath);
        return !rel.StartsWith("..", StringComparison.Ordinal)
            && !Path.IsPathRooted(rel);
    }

    private readonly record struct CacheEntry(string Body, DateTimeOffset ExpiresAtUtc);
}
