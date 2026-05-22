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
