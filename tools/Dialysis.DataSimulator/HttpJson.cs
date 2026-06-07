using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dialysis.DataSimulator;

/// <summary>HTTP+JSON helpers tolerant of the platform's HATEOAS <c>{ data: {...}, links: [] }</c> envelope.</summary>
public static class HttpJson
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>POSTs <paramref name="body"/> and returns the first matching id property (root or under <c>data</c>).</summary>
    public static async Task<Guid> PostReadIdAsync(
        HttpClient client, string requestUri, object body, CancellationToken cancellationToken, params string[] idPropertyNames)
    {
        var names = idPropertyNames.Length > 0 ? idPropertyNames : ["id"];
        using var response = await client.PostAsync(requestUri, JsonContent.Create(body, options: _options), cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, requestUri, cancellationToken).ConfigureAwait(false);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        foreach (var name in names)
        {
            if (TryFind(doc.RootElement, name, out var value) && value.TryGetGuid(out var id))
                return id;
        }
        throw new InvalidOperationException($"Response from {requestUri} did not contain an id ({string.Join('/', names)}).");
    }

    /// <summary>POSTs <paramref name="body"/> expecting no content (e.g. a 204).</summary>
    public static async Task PostAsync(HttpClient client, string requestUri, object? body, CancellationToken cancellationToken)
    {
        using var content = body is null ? null : JsonContent.Create(body, options: _options);
        using var response = await client.PostAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, requestUri, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>GETs and deserializes <typeparamref name="T"/> (e.g. a bare JSON array of read-model rows).</summary>
    public static async Task<T?> GetAsync<T>(HttpClient client, string requestUri, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, requestUri, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<T>(_options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>GETs and deserializes <typeparamref name="T"/>, unwrapping the HATEOAS <c>data</c> envelope when present.</summary>
    public static async Task<T?> GetEnvelopedAsync<T>(HttpClient client, string requestUri, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, requestUri, cancellationToken).ConfigureAwait(false);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var target = doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("data", out var data)
            ? data
            : doc.RootElement;
        return target.Deserialize<T>(_options);
    }

    /// <summary>POSTs <paramref name="body"/> with extra request headers (e.g. FHIR partner routing), expecting no usable body back.</summary>
    public static async Task PostWithHeadersAsync(
        HttpClient client, string requestUri, object body, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(headers);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body, options: _options),
        };
        foreach (var (key, value) in headers)
            request.Headers.TryAddWithoutValidation(key, value);
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, requestUri, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string requestUri, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new HttpRequestException(
            $"{requestUri} failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}",
            inner: null,
            statusCode: response.StatusCode);
    }

    private static bool TryFind(JsonElement root, string name, out JsonElement value)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty(name, out value))
                return true;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty(name, out value))
                return true;
        }
        value = default;
        return false;
    }
}
