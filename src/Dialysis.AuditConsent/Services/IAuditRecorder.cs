namespace Dialysis.AuditConsent.Services;

public interface IAuditRecorder
{
    Task RecordAsync(string resourceType, string resourceId, string action, string? agentId = null, string? outcome = null, CancellationToken cancellationToken = default);
}
