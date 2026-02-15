namespace Dialysis.Analytics.Services;

/// <summary>Records audit events for Analytics API calls (cohort, export). Sends to Dialysis.AuditConsent when configured.</summary>
public interface IAnalyticsAuditRecorder
{
    Task RecordAsync(string resourceType, string resourceId, string action, string? agentId = null, string? outcome = "0", CancellationToken cancellationToken = default);
}
