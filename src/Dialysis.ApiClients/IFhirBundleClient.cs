using Hl7.Fhir.Model;

namespace Dialysis.ApiClients;

/// <summary>Client for fetching FHIR Bundles from arbitrary URLs (pagination next links).</summary>
public interface IFhirBundleClient
{
    /// <summary>Fetches a Bundle from the given URL. URL may be absolute or relative to the configured base.</summary>
    Task<Bundle> GetBundleAsync(string requestUri, CancellationToken cancellationToken = default);
}
