using BuildingBlocks.Abstractions;

using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Audit;

/// <summary>
/// Records audit events via structured logging. Aligns with FHIR AuditEvent semantics.
/// C5 compliant: security-relevant actions are audited.
/// </summary>
public sealed class LoggingAuditRecorder : IAuditRecorder
{
    private readonly ILogger<LoggingAuditRecorder> _logger;

    public LoggingAuditRecorder(ILogger<LoggingAuditRecorder> logger)
    {
        _logger = logger;
    }

    public Task RecordAsync(AuditRecordRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AuditEvent: Action={Action} ResourceType={ResourceType} ResourceId={ResourceId} UserId={UserId} Outcome={Outcome} TenantId={TenantId}",
            request.Action, request.ResourceType, request.ResourceId ?? "(none)", request.UserId ?? "(system)", request.Outcome, request.TenantId ?? "default");
        return Task.CompletedTask;
    }
}
