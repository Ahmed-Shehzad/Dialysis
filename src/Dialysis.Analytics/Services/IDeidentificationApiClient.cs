namespace Dialysis.Analytics.Services;

/// <summary>Client for PublicHealth de-identification API. Used by research export.</summary>
public interface IDeidentificationApiClient
{
    Task<Stream?> DeidentifyAsync(Stream fhirJsonInput, string level = "Basic", CancellationToken cancellationToken = default);
}
