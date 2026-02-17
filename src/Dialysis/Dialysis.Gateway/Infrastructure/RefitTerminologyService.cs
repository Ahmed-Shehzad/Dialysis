using System.Text.Json;

using Dialysis.SharedKernel.Abstractions;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// Terminology lookup via FHIR $lookup. Phase 4.3.2.
/// Config: Terminology:ServerUrl (e.g. https://tx.fhir.org/r4).
/// </summary>
public sealed class RefitTerminologyService : ITerminologyService
{
    private readonly HttpClient _http;

    public RefitTerminologyService(HttpClient http)
    {
        _http = http;
    }

    public async Task<string?> LookupDisplayAsync(string system, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(system) || string.IsNullOrWhiteSpace(code))
            return null;
        try
        {
            var url = $"/CodeSystem/$lookup?system={Uri.EscapeDataString(system)}&code={Uri.EscapeDataString(code)}";
            var json = await _http.GetStringAsync(url, cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("parameter", out var paramArray))
            {
                foreach (var p in paramArray.EnumerateArray())
                {
                    if (p.TryGetProperty("name", out var name) && name.GetString() == "display"
                        && p.TryGetProperty("valueString", out var value))
                        return value.GetString();
                }
            }
        }
        catch
        {
            // Return null on any failure
        }
        return null;
    }
}
