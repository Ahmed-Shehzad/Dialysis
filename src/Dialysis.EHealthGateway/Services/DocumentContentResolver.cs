using Dialysis.ApiClients;
using Dialysis.EHealthGateway.Configuration;
using Microsoft.Extensions.Options;
using Refit;

namespace Dialysis.EHealthGateway.Services;

/// <summary>Resolves DocumentReference ID to PDF via Documents API or FHIR Gateway.</summary>
public sealed class DocumentContentResolver : IDocumentContentResolver
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly EHealthOptions _options;

    public DocumentContentResolver(IHttpClientFactory httpFactory, IOptions<EHealthOptions> options)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
    }

    public async Task<byte[]?> ResolveAsync(string documentReferenceId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_options.DocumentsBaseUrl))
        {
            var content = await ResolveViaDocumentsApiAsync(documentReferenceId, cancellationToken);
            if (content != null) return content;
        }

        if (!string.IsNullOrWhiteSpace(_options.FhirBaseUrl))
        {
            var content = await ResolveViaFhirAsync(documentReferenceId, cancellationToken);
            if (content != null) return content;
        }

        return null;
    }

    private async Task<byte[]?> ResolveViaDocumentsApiAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpFactory.CreateClient();
            client.BaseAddress = new Uri(_options.DocumentsBaseUrl!.TrimEnd('/') + "/");
            var api = RestService.For<IDocumentsApi>(client);
            var response = await api.GetContentAsync(id, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task<byte[]?> ResolveViaFhirAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpFactory.CreateClient();
            client.BaseAddress = new Uri(_options.FhirBaseUrl!.TrimEnd('/') + "/");
            var fhirApi = RestService.For<IFhirApi>(client);
            var docRef = await fhirApi.GetDocumentReference(id, cancellationToken);
            var attachment = docRef.Content?.FirstOrDefault()?.Attachment;
            if (attachment == null) return null;
            if (attachment.Data != null) return attachment.Data;
            if (string.IsNullOrEmpty(attachment.Url)) return null;

            var url = attachment.Url.StartsWith("/")
                ? _options.FhirBaseUrl!.TrimEnd('/') + attachment.Url
                : attachment.Url;

            var httpClient = _httpFactory.CreateClient();
            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
