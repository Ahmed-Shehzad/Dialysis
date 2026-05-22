using System.Net.Http.Headers;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Dialysis.SmartConnect.Adapters;

/// <summary>
/// Reusable base for vendor adapters that talk plain FHIR REST over HTTPS. Subclasses supply the
/// <see cref="IExternalEhrAuthProvider"/> and the vendor descriptor.
/// </summary>
public abstract class HttpFhirAdapterBase(IHttpClientFactory httpClientFactory, IExternalEhrAuthProvider authProvider) : IExternalEhrAdapter
{
    private static readonly JsonSerializerOptions _fhirJson =
        new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);

    public abstract ExternalEhrAdapterDescriptor Describe();

    public async Task<TResource> ReadAsync<TResource>(string id, ExternalEhrContext context, CancellationToken cancellationToken)
        where TResource : Resource
    {
        var sample = (TResource)Activator.CreateInstance(typeof(TResource))!;
        var url = $"{Describe().BaseUrl.TrimEnd('/')}/{sample.TypeName}/{id}";
        using var response = await SendAsync(HttpMethod.Get, url, context, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<TResource>(json, _fhirJson)!;
    }

    public async Task<Bundle> SearchAsync(string resourceType, IDictionary<string, string> parameters, ExternalEhrContext context, CancellationToken cancellationToken)
    {
        var query = string.Join('&', parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        var url = $"{Describe().BaseUrl.TrimEnd('/')}/{resourceType}{(query.Length > 0 ? "?" + query : string.Empty)}";
        using var response = await SendAsync(HttpMethod.Get, url, context, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<Bundle>(json, _fhirJson)!;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, ExternalEhrContext context, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(Describe().VendorName);
        var token = await authProvider.AcquireAccessTokenAsync(context, cancellationToken).ConfigureAwait(false);
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (context.Headers is not null)
        {
            foreach (var (key, value) in context.Headers)
                request.Headers.TryAddWithoutValidation(key, value);
        }
        return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
