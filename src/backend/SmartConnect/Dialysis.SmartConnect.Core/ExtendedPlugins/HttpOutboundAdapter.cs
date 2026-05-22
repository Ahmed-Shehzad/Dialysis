using System.Net.Http.Headers;
using System.Text.Json;
using Dialysis.SmartConnect.Authentication;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>POST/PUT raw payload to a configured URL (JSON in <c>smartconnect.outbound.parameters</c> on the message).</summary>
public sealed class HttpOutboundAdapter(
    IHttpClientFactory httpClientFactory,
    IHttpAuthenticationProviderRegistry authenticationRegistry) : IOutboundAdapter
{
    public const string ParametersMetadataKey = "smartconnect.outbound.parameters";

    public string Kind => "http";

    public async Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(ParametersMetadataKey, out var json) || string.IsNullOrWhiteSpace(json))
        {
            return new OutboundSendResult(false, "HTTP outbound requires parameters JSON with Url.");
        }

        var opts = JsonSerializer.Deserialize<HttpOutboundParameters>(json);
        if (opts is null || string.IsNullOrWhiteSpace(opts.Url))
        {
            return new OutboundSendResult(false, "HTTP outbound parameters must include Url.");
        }

        var method = string.IsNullOrWhiteSpace(opts.Method) ? HttpMethod.Post : new HttpMethod(opts.Method.Trim());
        var client = httpClientFactory.CreateClient("smartconnect-outbound");
        using var request = new HttpRequestMessage(method, opts.Url);
        var body = new ByteArrayContent(message.Payload.ToArray());
        if (opts.Headers is not null)
        {
            foreach (var (k, v) in opts.Headers)
            {
                if (k.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    body.Headers.ContentType = MediaTypeHeaderValue.Parse(v);
                }
                else if (!request.Headers.TryAddWithoutValidation(k, v))
                {
                    body.Headers.TryAddWithoutValidation(k, v);
                }
            }
        }

        if (body.Headers.ContentType is null)
        {
            body.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        }

        request.Content = body;

        if (opts.Authentication is { } auth)
        {
            if (!authenticationRegistry.TryGet(auth.Kind, out var provider))
            {
                return new OutboundSendResult(false, $"Unknown HTTP authentication kind '{auth.Kind}'.");
            }

            try
            {
                await provider.ApplyAsync(request, auth.ParametersJson, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is InvalidOperationException or JsonException or HttpRequestException)
            {
                return new OutboundSendResult(false, $"HTTP authentication '{auth.Kind}' failed: {ex.Message}");
            }
        }

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? new OutboundSendResult(true, null)
            : new OutboundSendResult(false, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
    }
}
