using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Audit;

public sealed class DefaultAuditEventEmitter : IAuditEventEmitter
{
    private readonly IAuditEventStore _store;
    public DefaultAuditEventEmitter(IAuditEventStore store) => _store = store;
    public ValueTask EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        => _store.AppendAsync(auditEvent, cancellationToken);
}
