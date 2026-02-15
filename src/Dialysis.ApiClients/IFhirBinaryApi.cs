using Refit;

namespace Dialysis.ApiClients;

/// <summary>Refit client for FHIR Binary operations (create, fetch by path).</summary>
public interface IFhirBinaryApi
{
    [Post("Binary")]
    Task<HttpResponseMessage> CreateBinaryAsync(
        [Body] Stream content,
        [Header("Content-Type")] string contentType,
        CancellationToken cancellationToken = default);

    [Get("{*path}")]
    Task<byte[]> GetAsync(string path, CancellationToken cancellationToken = default);
}
