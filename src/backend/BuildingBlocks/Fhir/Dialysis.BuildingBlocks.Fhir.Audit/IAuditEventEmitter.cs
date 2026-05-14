using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Audit;

public interface IAuditEventEmitter
{
    ValueTask EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}

public interface IAuditEventStore
{
    ValueTask AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
