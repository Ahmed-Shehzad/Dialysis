using System.Net;
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

    /// <summary>Standard transient set per Mirth UG retry guidance — request timeout, throttle, and 5xx server errors.</summary>
    private static readonly int[] DefaultRetryStatusCodes =
    [
        (int)HttpStatusCode.RequestTimeout,
        (int)HttpStatusCode.TooManyRequests,
        (int)HttpStatusCode.InternalServerError,
        (int)HttpStatusCode.BadGateway,
        (int)HttpStatusCode.ServiceUnavailable,
        (int)HttpStatusCode.GatewayTimeout,
    ];

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

        var client = httpClientFactory.CreateClient("smartconnect-outbound");
        var connector = opts.ConnectorProperties ?? new HttpConnectorProperties();
        var maxRetries = Math.Max(0, connector.MaxRetries ?? 0);
        var retryDelayMs = Math.Max(0, connector.RetryDelayMs ?? 500);
        var retryStatusCodes = connector.RetryOnStatusCodes is { Length: > 0 }
            ? connector.RetryOnStatusCodes
            : DefaultRetryStatusCodes;

        OutboundSendResult lastResult = default;
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            if (attempt > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(retryDelayMs * attempt), cancellationToken).ConfigureAwait(false);
            }

            var (result, statusCode, terminal) = await SendOnceAsync(
                opts, message, client, connector, cancellationToken).ConfigureAwait(false);
            if (result.Succeeded || terminal)
            {
                return result;
            }

            lastResult = result;
            var isRetryable = statusCode.HasValue && Array.IndexOf(retryStatusCodes, (int)statusCode.Value) >= 0;
            if (!isRetryable)
            {
                return result;
            }
        }

        return lastResult;
    }

    private async Task<(OutboundSendResult Result, HttpStatusCode? StatusCode, bool Terminal)> SendOnceAsync(
        HttpOutboundParameters opts,
        IntegrationMessage message,
        HttpClient client,
        HttpConnectorProperties connector,
        CancellationToken cancellationToken)
    {
        var method = string.IsNullOrWhiteSpace(opts.Method) ? HttpMethod.Post : new HttpMethod(opts.Method.Trim());
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

        body.Headers.ContentType ??= new MediaTypeHeaderValue("application/octet-stream");
        request.Content = body;

        if (opts.Authentication is { } auth)
        {
            if (!authenticationRegistry.TryGet(auth.Kind, out var provider))
            {
                // Misconfiguration — no amount of retry will fix it; mark terminal.
                return (new OutboundSendResult(false, $"Unknown HTTP authentication kind '{auth.Kind}'."), null, true);
            }

            try
            {
                await provider.ApplyAsync(request, auth.ParametersJson, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is InvalidOperationException or JsonException or HttpRequestException)
            {
                return (new OutboundSendResult(false, $"HTTP authentication '{auth.Kind}' failed: {ex.Message}"), null, true);
            }
        }

        using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (connector.TimeoutSeconds is { } seconds and > 0)
        {
            attemptCts.CancelAfter(TimeSpan.FromSeconds(seconds));
        }

        try
        {
            using var response = await client.SendAsync(request, attemptCts.Token).ConfigureAwait(false);
            byte[]? responsePayload = null;
            if (connector.CaptureResponseBody)
            {
                responsePayload = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            }

            if (response.IsSuccessStatusCode)
            {
                return (new OutboundSendResult(true, null, responsePayload), response.StatusCode, false);
            }

            return (
                new OutboundSendResult(false, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}", responsePayload),
                response.StatusCode,
                false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Per-attempt timeout (the caller's token isn't yet cancelled); surface as a 408
            // so the retry policy can decide whether to back off and try again.
            return (
                new OutboundSendResult(false, $"HTTP request exceeded TimeoutSeconds={connector.TimeoutSeconds}."),
                HttpStatusCode.RequestTimeout,
                false);
        }
    }
}
