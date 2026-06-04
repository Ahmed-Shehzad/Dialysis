using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Dialysis.SmartConnect.Authentication;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>POST/PUT raw payload to a configured URL (JSON in <c>smartconnect.outbound.parameters</c> on the message).</summary>
public sealed class HttpOutboundAdapter : IOutboundAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpAuthenticationProviderRegistry _authenticationRegistry;
    /// <summary>POST/PUT raw payload to a configured URL (JSON in <c>smartconnect.outbound.parameters</c> on the message).</summary>
    public HttpOutboundAdapter(IHttpClientFactory httpClientFactory,
        IHttpAuthenticationProviderRegistry authenticationRegistry)
    {
        _httpClientFactory = httpClientFactory;
        _authenticationRegistry = authenticationRegistry;
    }
    public const string ParametersMetadataKey = "smartconnect.outbound.parameters";

    /// <summary>Standard transient set per Mirth UG retry guidance — request timeout, throttle, and 5xx server errors.</summary>
    private static readonly int[] _defaultRetryStatusCodes =
    [
        (int)HttpStatusCode.RequestTimeout,
        (int)HttpStatusCode.TooManyRequests,
        (int)HttpStatusCode.InternalServerError,
        (int)HttpStatusCode.BadGateway,
        (int)HttpStatusCode.ServiceUnavailable,
        (int)HttpStatusCode.GatewayTimeout,
    ];

    public string Kind => "http";

    /// <summary>Slice B2: JSON Schema describing the HTTP outbound parameters shape so
    /// the operator-shell can render a form-driven editor instead of raw-JSON.</summary>
    public string? GetParametersSchema() => HttpOutboundParametersSchema;

    private const string HttpOutboundParametersSchema = """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "title": "HttpOutboundParameters",
          "type": "object",
          "required": ["Url"],
          "properties": {
            "Url": {
              "type": "string",
              "format": "uri",
              "description": "Absolute HTTP/HTTPS endpoint of the partner system."
            },
            "Method": {
              "type": "string",
              "enum": ["GET", "POST", "PUT", "PATCH", "DELETE"],
              "default": "POST",
              "description": "HTTP verb used for the outbound send."
            },
            "Headers": {
              "type": "object",
              "description": "Custom request headers (case-insensitive keys). Content-Type may be set here to override the default application/octet-stream.",
              "additionalProperties": { "type": "string" }
            },
            "Authentication": {
              "type": "object",
              "description": "Per-route authentication block (slice A / A2). Kind selects the IHttpAuthenticationProvider; remaining fields are passed verbatim to the provider.",
              "required": ["Kind"],
              "properties": {
                "Kind": {
                  "type": "string",
                  "enum": ["bearer", "api-key", "basic", "oauth2-client-credentials", "mutual-tls"]
                }
              },
              "additionalProperties": true
            },
            "ConnectorProperties": {
              "type": "object",
              "description": "Slice B per-route connector tuning.",
              "properties": {
                "TimeoutSeconds": { "type": "integer", "minimum": 1, "description": "Per-attempt deadline." },
                "MaxRetries": { "type": "integer", "minimum": 0, "default": 0 },
                "RetryDelayMs": { "type": "integer", "minimum": 0, "default": 500 },
                "RetryOnStatusCodes": {
                  "type": "array",
                  "items": { "type": "integer" },
                  "description": "HTTP status codes that trigger a retry. Defaults to 408 + 429 + 500/502/503/504 when omitted."
                },
                "CaptureResponseBody": {
                  "type": "boolean",
                  "default": false,
                  "description": "Surface the response body in OutboundSendResult.ResponsePayload so response-transform stages and ledger inspectors can see it."
                }
              }
            }
          }
        }
        """;

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

        var client = _httpClientFactory.CreateClient("smartconnect-outbound");
        var connector = opts.ConnectorProperties ?? new HttpConnectorProperties();
        var maxRetries = Math.Max(0, connector.MaxRetries ?? 0);
        var retryDelayMs = Math.Max(0, connector.RetryDelayMs ?? 500);
        var retryStatusCodes = connector.RetryOnStatusCodes is { Length: > 0 }
            ? connector.RetryOnStatusCodes
            : _defaultRetryStatusCodes;

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
            if (!_authenticationRegistry.TryGet(auth.Kind, out var provider))
            {
                // Misconfiguration — no amount of retry will fix it; mark terminal.
                return (new OutboundSendResult(false, $"Unknown HTTP authentication kind '{auth.Kind}'."), null, true);
            }

            try
            {
                // Slice A2: handler-bound auth schemes (mutual TLS, future SPNEGO) swap the
                // HttpClient itself rather than mutating the request. Header-based providers
                // (Bearer / API key / Basic / OAuth2) return null and keep the named client.
                var resolved = await provider
                    .ResolveClientAsync(auth.ParametersJson, client, cancellationToken)
                    .ConfigureAwait(false);
                if (resolved is not null)
                {
                    client = resolved;
                }

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
