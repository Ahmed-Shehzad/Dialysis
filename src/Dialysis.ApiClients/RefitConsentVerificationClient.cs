namespace Dialysis.ApiClients;

/// <summary>Consent verification via Refit IAuditConsentApi.</summary>
public sealed class RefitConsentVerificationClient : IConsentVerificationClient
{
    private readonly IAuditConsentApi _api;

    public RefitConsentVerificationClient(IAuditConsentApi api) => _api = api;

    public async Task<bool> HasConsentAsync(string resourceType, string resourceId, string action = "consent-granted", CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _api.CheckConsentAsync(resourceType, resourceId, action, cancellationToken);
            return result.HasConsent;
        }
        catch
        {
            return false;
        }
    }
}
