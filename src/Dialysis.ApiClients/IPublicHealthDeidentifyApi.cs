using Refit;

namespace Dialysis.ApiClients;

/// <summary>Refit client for PublicHealth de-identification API.</summary>
public interface IPublicHealthDeidentifyApi
{
    [Post("api/v1/deidentify")]
    Task<Stream> DeidentifyAsync(
        [Body] Stream fhirJsonInput,
        [Header("Content-Type")] string contentType,
        [Query] string level = "Basic",
        CancellationToken cancellationToken = default);
}
