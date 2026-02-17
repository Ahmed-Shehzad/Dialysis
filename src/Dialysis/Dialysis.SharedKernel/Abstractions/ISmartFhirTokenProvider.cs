namespace Dialysis.SharedKernel.Abstractions;

/// <summary>
/// Obtains OAuth2 access tokens for SMART on FHIR EHR calls. Used when PDMS acts as FHIR client.
/// </summary>
public interface ISmartFhirTokenProvider
{
    /// <summary>
    /// Returns true if SMART OAuth2 is configured (client credentials or similar).
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gets a valid access token for calling the EHR FHIR API. Returns null if not configured.
    /// </summary>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
