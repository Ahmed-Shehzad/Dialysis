using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Audit;

public sealed class DefaultAuditEventEmitter(IAuditEventStore store) : IAuditEventEmitter
{
    public ValueTask EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        => store.AppendAsync(auditEvent, cancellationToken);
}
