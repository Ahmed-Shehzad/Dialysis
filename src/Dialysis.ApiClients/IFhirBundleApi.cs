using System.Net.Http;
using Refit;

namespace Dialysis.ApiClients;

/// <summary>Refit client for fetching FHIR Bundles from paths (used by IFhirBundleClient adapter).</summary>
public interface IFhirBundleApi
{
    [Get("{*path}")]
    Task<HttpResponseMessage> GetAsync(
        string path,
        [Query] IDictionary<string, string>? query,
        CancellationToken cancellationToken = default);
}
