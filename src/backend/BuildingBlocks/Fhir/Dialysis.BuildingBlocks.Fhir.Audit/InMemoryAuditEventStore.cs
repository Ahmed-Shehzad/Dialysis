using System.Collections.Concurrent;
using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Audit;

/// <summary>
/// In-memory ring buffer of recent audit events. Suitable for development; production should swap in
/// the EF-Core-backed store from <c>Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore</c>.
/// </summary>
public sealed class InMemoryAuditEventStore : IAuditEventStore
{
    private readonly ConcurrentQueue<AuditEvent> _events = new();
    private readonly int _capacity;
    /// <summary>
    /// In-memory ring buffer of recent audit events. Suitable for development; production should swap in
    /// the EF-Core-backed store from <c>Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore</c>.
    /// </summary>
    public InMemoryAuditEventStore(int capacity = 10_000) => _capacity = capacity;

    public ValueTask AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        _events.Enqueue(auditEvent);
        while (_events.Count > _capacity && _events.TryDequeue(out _))
        {
            // Trimming happens in the loop condition itself.
        }
        return ValueTask.CompletedTask;
    }

    public IEnumerable<AuditEvent> Snapshot() => [.. _events];
}
