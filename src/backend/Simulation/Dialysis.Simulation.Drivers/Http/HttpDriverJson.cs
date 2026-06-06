using System.Net.Http.Json;
using System.Text.Json;

namespace Dialysis.Simulation.Drivers.Http;

/// <summary>
/// Small HTTP+JSON helpers shared by the HTTP drivers. Tolerant of the platform's HATEOAS envelope:
/// values are read from the JSON root or from a wrapping <c>data</c> object.
/// </summary>
internal static class HttpDriverJson
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public static async Task<Guid> PostReadIdAsync(
        HttpClient client, string requestUri, object body, DriverContext context, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body, options: _serializerOptions),
        };
        Stamp(request, context);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await ReadIdAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public static async Task PostNoContentAsync(
        HttpClient client, string requestUri, DriverContext context, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        Stamp(request, context);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public static async Task<string> GetStringPropAsync(
        HttpClient client, string requestUri, string propertyName, DriverContext context, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        Stamp(request, context);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return FindProperty(doc.RootElement, propertyName)?.GetString()
            ?? throw new InvalidOperationException($"Response from {requestUri} did not contain '{propertyName}'.");
    }

    private static async Task<Guid> ReadIdAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var idElement = FindProperty(doc.RootElement, "id")
            ?? throw new InvalidOperationException("Response did not contain an 'id'.");
        return idElement.GetGuid();
    }

    private static JsonElement? FindProperty(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty(name, out var direct))
                return direct;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty(name, out var nested))
                return nested;
        }
        return null;
    }

    private static void Stamp(HttpRequestMessage request, DriverContext context)
    {
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", context.CorrelationId);
        request.Headers.TryAddWithoutValidation("X-Tenant-Id", context.TenantId);
    }
}
