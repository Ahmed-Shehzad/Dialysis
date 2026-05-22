using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dialysis.SmartConnect.ExtendedPlugins;

internal sealed class HttpOutboundParameters
{
    public string? Url { get; set; }

    public string? Method { get; set; }

    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Optional per-route authentication block. <c>Kind</c> selects the
    /// <see cref="Authentication.IHttpAuthenticationProvider"/> registered with the runtime
    /// (e.g. <c>bearer</c>, <c>api-key</c>, <c>basic</c>, <c>oauth2-client-credentials</c>); the
    /// remaining properties are passed verbatim as a JSON object so each provider can deserialise its
    /// own option shape.
    /// </summary>
    [JsonConverter(typeof(AuthenticationParametersJsonConverter))]
    public AuthenticationParameters? Authentication { get; set; }

    /// <summary>
    /// Optional per-route connector tuning — timeouts, retry policy, response capture. Maps
    /// 1:1 to the Mirth Connect Destination Connector Properties pane (UG pp. 246–252). All
    /// properties are optional; omitted values inherit the adapter's defaults (no per-request
    /// timeout beyond the named <c>HttpClient</c>, no retries, no response body capture).
    /// </summary>
    public HttpConnectorProperties? ConnectorProperties { get; set; }
}

internal sealed class HttpConnectorProperties
{
    /// <summary>Total deadline for the request including connect/send/receive. <c>null</c> means
    /// fall back to the named <c>HttpClient</c>'s default timeout.</summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>Maximum retry attempts after the first failed call. Defaults to <c>0</c> when
    /// unset, preserving the previous "send once" behaviour.</summary>
    public int? MaxRetries { get; set; }

    /// <summary>Delay between retries in milliseconds. Each retry waits <c>RetryDelayMs * attempt</c>
    /// for a coarse linear backoff (a future slice can swap in jittered exponential backoff).</summary>
    public int? RetryDelayMs { get; set; }

    /// <summary>HTTP status codes that trigger a retry. Defaults to the standard transient set
    /// (408 Request Timeout, 429 Too Many Requests, 500/502/503/504) when omitted.</summary>
    public int[]? RetryOnStatusCodes { get; set; }

    /// <summary>When <c>true</c>, surfaces the response body in <see cref="OutboundSendResult.ResponsePayload"/>
    /// so downstream response-transform stages and ledger inspectors can see it. Off by default
    /// because most partners reply with empty 200/201 bodies and the bytes would just bloat
    /// the ledger.</summary>
    public bool CaptureResponseBody { get; set; }
}

internal sealed class AuthenticationParameters
{
    public required string Kind { get; init; }

    /// <summary>
    /// Raw JSON object payload retained so the matching
    /// <see cref="Authentication.IHttpAuthenticationProvider"/> can parse its provider-specific
    /// option shape without the outbound adapter needing to know about Bearer/ApiKey/Basic/OAuth2.
    /// </summary>
    public required string ParametersJson { get; init; }
}

internal sealed class AuthenticationParametersJsonConverter : JsonConverter<AuthenticationParameters>
{
    public override AuthenticationParameters? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("Authentication parameters must be a JSON object.");

        if (!root.TryGetProperty("Kind", out var kindElement) || kindElement.ValueKind != JsonValueKind.String)
            throw new JsonException("Authentication parameters must include a string 'Kind'.");

        return new AuthenticationParameters
        {
            Kind = kindElement.GetString()!,
            ParametersJson = root.GetRawText(),
        };
    }

    public override void Write(Utf8JsonWriter writer, AuthenticationParameters value, JsonSerializerOptions options)
    {
        using var document = JsonDocument.Parse(value.ParametersJson);
        document.RootElement.WriteTo(writer);
    }
}
