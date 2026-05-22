using System.Text.Json;
using Dialysis.SmartConnect.Authentication;

namespace Dialysis.SmartConnect.ExtendedPlugins.Authentication;

/// <summary>
/// Attaches an API key as a request header. Both <c>HeaderName</c> and <c>Value</c> are required; the
/// default header convention is <c>X-API-Key</c>, but a per-route override lets each partner use its
/// own (e.g. Mayo Clinic uses <c>Ocp-Apim-Subscription-Key</c>).
/// </summary>
public sealed class ApiKeyAuthenticationProvider : IHttpAuthenticationProvider
{
    public string Kind => "api-key";

    public Task ApplyAsync(HttpRequestMessage request, string parametersJson, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var options = JsonSerializer.Deserialize<ApiKeyOptions>(parametersJson)
            ?? throw new InvalidOperationException("API-key authentication parameters must be a JSON object.");
        if (string.IsNullOrWhiteSpace(options.Value))
            throw new InvalidOperationException("API-key authentication parameters must include 'Value'.");

        var header = string.IsNullOrWhiteSpace(options.HeaderName) ? "X-API-Key" : options.HeaderName;
        request.Headers.TryAddWithoutValidation(header, options.Value);
        return Task.CompletedTask;
    }

    private sealed class ApiKeyOptions
    {
        public string? HeaderName { get; set; }

        public string? Value { get; set; }
    }
}
