namespace Dialysis.ApiClients;

/// <summary>No-op consent client that always returns true when AuditConsent is not configured.</summary>
public sealed class NoOpConsentVerificationClient : IConsentVerificationClient
{
    public Task<bool> HasConsentAsync(string resourceType, string resourceId, string action = "consent-granted", CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
