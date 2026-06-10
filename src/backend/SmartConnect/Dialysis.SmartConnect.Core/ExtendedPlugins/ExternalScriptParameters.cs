using System.Text.Json;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Shared parser for the parameters JSON used by both external-script plugins.
/// Shape: <c>{"scriptUri":"file:///... | http(s)://...","cacheTtlSeconds":60}</c>.
/// Returns (null, null) when <c>scriptUri</c> is missing or unparseable, so callers
/// can no-op rather than throw on misconfigured slots.
/// </summary>
internal static class ExternalScriptParameters
{
    public static (Uri? Uri, TimeSpan? CacheTtl) Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (null, null);

        JsonDocument doc;
        try
        { doc = JsonDocument.Parse(json); }
        catch (JsonException) { return (null, null); }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return (null, null);
            if (!doc.RootElement.TryGetProperty("scriptUri", out var uriEl) || uriEl.ValueKind != JsonValueKind.String)
                return (null, null);
            var raw = uriEl.GetString();
            if (string.IsNullOrWhiteSpace(raw))
                return (null, null);
            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
                return (null, null);

            TimeSpan? ttl = null;
            if (doc.RootElement.TryGetProperty("cacheTtlSeconds", out var ttlEl) && ttlEl.ValueKind == JsonValueKind.Number && ttlEl.TryGetDouble(out var seconds) && seconds >= 0)
            {
                ttl = TimeSpan.FromSeconds(seconds);
            }
            return (uri, ttl);
        }
    }
}
