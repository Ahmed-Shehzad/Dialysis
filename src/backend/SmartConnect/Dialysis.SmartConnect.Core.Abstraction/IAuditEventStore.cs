namespace Dialysis.SmartConnect;

/// <summary>
/// Persists and queries audit events.
/// </summary>
public interface IAuditEventStore
{
    Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEvent>> QueryAsync(
        AuditEventCategory? category = null,
        AuditEventLevel? level = null,
        Guid? flowId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<AuditEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
