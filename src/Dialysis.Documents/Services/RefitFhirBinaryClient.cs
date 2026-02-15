using Dialysis.ApiClients;

namespace Dialysis.Documents.Services;

/// <summary>FHIR Binary operations via Refit IFhirBinaryApi.</summary>
public interface IFhirBinaryClient
{
    Task<string> CreateBinaryAsync(byte[] content, string contentType, CancellationToken cancellationToken = default);
    Task<byte[]?> GetAsync(string pathOrUrl, CancellationToken cancellationToken = default);
}

public sealed class RefitFhirBinaryClient : IFhirBinaryClient
{
    private readonly IFhirBinaryApi _api;
    private readonly IHttpClientFactory _httpFactory;

    public RefitFhirBinaryClient(IFhirBinaryApi api, IHttpClientFactory httpFactory)
    {
        _api = api;
        _httpFactory = httpFactory;
    }

    public async Task<string> CreateBinaryAsync(byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content);
        var response = await _api.CreateBinaryAsync(stream, contentType, cancellationToken);
        response.EnsureSuccessStatusCode();
        var location = response.Headers.Location?.ToString() ?? "";
        return location.Split('/').LastOrDefault() ?? "";
    }

    public async Task<byte[]?> GetAsync(string pathOrUrl, CancellationToken cancellationToken = default)
    {
        if (pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var client = _httpFactory.CreateClient();
            var response = await client.GetAsync(pathOrUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        var path = pathOrUrl.TrimStart('/');
        return await _api.GetAsync(path, cancellationToken);
    }
}
