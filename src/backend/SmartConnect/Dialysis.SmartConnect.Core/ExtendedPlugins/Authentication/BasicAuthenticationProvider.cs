using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.Authentication;

namespace Dialysis.SmartConnect.ExtendedPlugins.Authentication;

/// <summary>
/// HTTP Basic authentication — base64-encoded <c>user:password</c> in the <c>Authorization</c> header.
/// Still used by older lab partners and on-prem appliances; never to be used over plain HTTP.
/// </summary>
public sealed class BasicAuthenticationProvider : IHttpAuthenticationProvider
{
    public string Kind => "basic";

    public Task ApplyAsync(HttpRequestMessage request, string parametersJson, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var options = JsonSerializer.Deserialize<BasicOptions>(parametersJson)
            ?? throw new InvalidOperationException("Basic authentication parameters must be a JSON object.");
        if (string.IsNullOrWhiteSpace(options.Username))
            throw new InvalidOperationException("Basic authentication parameters must include 'Username'.");

        var encoded = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{options.Username}:{options.Password ?? string.Empty}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        return Task.CompletedTask;
    }

    private sealed class BasicOptions
    {
        public string? Username { get; set; }

        public string? Password { get; set; }
    }
}
