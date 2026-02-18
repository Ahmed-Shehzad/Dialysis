using BuildingBlocks.Abstractions;

using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Audit;

/// <summary>
/// Records audit events to a store and via structured logging.
/// Stored events can be retrieved and mapped to FHIR AuditEvent for C5 compliance.
/// </summary>
public sealed class FhirAuditRecorder : IAuditRecorder
{
    private readonly IAuditEventStore _store;
    private readonly ILogger<FhirAuditRecorder> _logger;

    public FhirAuditRecorder(IAuditEventStore store, ILogger<FhirAuditRecorder> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task RecordAsync(AuditRecordRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AuditEvent: Action={Action} ResourceType={ResourceType} ResourceId={ResourceId} UserId={UserId} Outcome={Outcome} TenantId={TenantId}",
            request.Action, request.ResourceType, request.ResourceId ?? "(none)", request.UserId ?? "(system)", request.Outcome, request.TenantId ?? "default");

        await _store.AppendAsync(request, cancellationToken);
    }
}
