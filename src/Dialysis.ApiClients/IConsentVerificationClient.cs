namespace Dialysis.ApiClients;

/// <summary>Verifies consent for a resource/action via AuditConsent service (e.g. research-export, ehealth-upload).</summary>
public interface IConsentVerificationClient
{
    /// <summary>Check if consent exists for the given resource and action. Returns false if service unavailable or no consent.</summary>
    Task<bool> HasConsentAsync(string resourceType, string resourceId, string action = "consent-granted", CancellationToken cancellationToken = default);
}
