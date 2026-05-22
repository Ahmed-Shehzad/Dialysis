using System.Net.Http.Headers;
using System.Text.Json;
using Dialysis.SmartConnect.Authentication;

namespace Dialysis.SmartConnect.ExtendedPlugins.Authentication;

/// <summary>
/// Attaches a static <c>Authorization: Bearer &lt;token&gt;</c> header. The token is supplied per-route
/// in the JSON parameters (operators source it from a secret store or a config map). For dynamic
/// tokens acquired at runtime use <see cref="OAuth2ClientCredentialsAuthenticationProvider"/>.
/// </summary>
public sealed class BearerTokenAuthenticationProvider : IHttpAuthenticationProvider
{
    public string Kind => "bearer";

    public Task ApplyAsync(HttpRequestMessage request, string parametersJson, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var options = JsonSerializer.Deserialize<BearerTokenOptions>(parametersJson)
            ?? throw new InvalidOperationException("Bearer authentication parameters must be a JSON object.");
        if (string.IsNullOrWhiteSpace(options.Token))
            throw new InvalidOperationException("Bearer authentication parameters must include 'Token'.");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Token);
        return Task.CompletedTask;
    }

    private sealed class BearerTokenOptions
    {
        public string? Token { get; set; }
    }
}
