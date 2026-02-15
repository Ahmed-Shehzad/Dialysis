namespace Dialysis.Analytics.Services;

public sealed class NoOpAnalyticsAuditRecorder : IAnalyticsAuditRecorder
{
    public Task RecordAsync(string resourceType, string resourceId, string action, string? agentId = null, string? outcome = "0", CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
