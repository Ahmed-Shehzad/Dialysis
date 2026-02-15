using Refit;

namespace Dialysis.ApiClients;

/// <summary>Refit client for AuditConsent service – record audit events and check consent.</summary>
public interface IAuditConsentApi
{
    [Post("api/v1/audit")]
    Task RecordAuditAsync([Body] RecordAuditRequest body, CancellationToken cancellationToken = default);

    [Get("api/v1/audit/consent")]
    Task<ConsentCheckResponse> CheckConsentAsync(
        [Query] string resourceType,
        [Query] string resourceId,
        [Query] string action = "consent-granted",
        CancellationToken cancellationToken = default);
}

public sealed record RecordAuditRequest(
    string ResourceType,
    string ResourceId,
    string Action,
    string? AgentId,
    string? Outcome);

public sealed record ConsentCheckResponse(bool HasConsent);
