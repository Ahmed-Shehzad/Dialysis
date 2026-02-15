using System.Net;
using Dialysis.Analytics.Configuration;
using Dialysis.ApiClients;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Options;

namespace Dialysis.Analytics.Services;

/// <summary>Fetches FHIR Bundles via Refit IFhirBundleApi.</summary>
public sealed class RefitFhirBundleClient : IFhirBundleClient
{
    private readonly IFhirBundleApi _api;
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _baseUrl;
    private readonly FhirJsonDeserializer _deserializer = new();

    public RefitFhirBundleClient(IFhirBundleApi api, IHttpClientFactory httpFactory, IOptions<AnalyticsOptions> options)
    {
        _api = api;
        _httpFactory = httpFactory;
        _baseUrl = options.Value.FhirBaseUrl.TrimEnd('/') + "/";
    }

    public async Task<Bundle> GetBundleAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        var url = requestUri.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? requestUri
            : _baseUrl + requestUri.TrimStart('/');

        // Refit IFhirBundleApi uses the same base; for absolute URLs to different hosts, use raw HttpClient
        if (requestUri.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var httpClient = _httpFactory.CreateClient();
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return _deserializer.Deserialize<Bundle>(json);
        }

        var uri = new Uri(url);
        var path = uri.AbsolutePath.TrimStart('/');
        var query = ParseQuery(uri.Query);

        var apiResponse = await _api.GetAsync(path, query.Count > 0 ? query : null, cancellationToken);
        apiResponse.EnsureSuccessStatusCode();
        var json2 = await apiResponse.Content.ReadAsStringAsync(cancellationToken);
        return _deserializer.Deserialize<Bundle>(json2);
    }

    private static Dictionary<string, string> ParseQuery(string queryString)
    {
        var dict = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(queryString) || queryString.Length < 2) return dict;
        var trimmed = queryString.StartsWith('?') ? queryString[1..] : queryString;
        foreach (var pair in trimmed.Split('&'))
        {
            var eq = pair.IndexOf('=');
            if (eq < 0) continue;
            var key = WebUtility.UrlDecode(pair[..eq]);
            var value = WebUtility.UrlDecode(pair[(eq + 1)..]);
            if (!string.IsNullOrEmpty(key)) dict[key] = value ?? "";
        }
        return dict;
    }
}
