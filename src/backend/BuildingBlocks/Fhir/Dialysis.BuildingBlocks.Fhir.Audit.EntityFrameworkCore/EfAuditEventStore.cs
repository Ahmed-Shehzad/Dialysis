using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;

/// <summary>
/// Persists <see cref="AuditEvent"/> resources as <see cref="AuditEventRecord"/> rows on the host
/// module's <typeparamref name="TDbContext"/>. Modules add
/// <see cref="AuditEventRecordConfiguration"/> to their <c>OnModelCreating</c> to enable this store.
/// </summary>
public sealed class EfAuditEventStore<TDbContext>(TDbContext db) : IAuditEventStore
    where TDbContext : DbContext
{
    private static readonly FhirJsonSerializer _serializer = new();

    public async ValueTask AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var firstEntity = auditEvent.Entity.Count > 0 ? auditEvent.Entity[0] : null;
        var resourceType = ExtractResourceType(firstEntity?.What?.Reference);
        var resourceId = ExtractResourceId(firstEntity?.What?.Reference);

        var record = new AuditEventRecord
        {
            Id = Guid.CreateVersion7(),
            RecordedAt = auditEvent.Recorded ?? DateTimeOffset.UtcNow,
            ModuleSlug = auditEvent.Source?.Site ?? "unknown",
            Subtype = auditEvent.Subtype.Count > 0 ? auditEvent.Subtype[0].Code ?? "unknown" : "unknown",
            AgentId = ExtractAgentId(auditEvent),
            ResourceType = resourceType,
            ResourceId = resourceId,
            Outcome = auditEvent.Outcome?.ToString() ?? "0",
#pragma warning disable VSTHRD103 // Firely SerializeToString is CPU-only; its *Async sibling is [Obsolete] (CodeQL cs/call-to-obsolete-method)
            ResourceJson = _serializer.SerializeToString(auditEvent),
#pragma warning restore VSTHRD103
        };

        db.Set<AuditEventRecord>().Add(record);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string? ExtractResourceType(string? reference)
    {
        if (string.IsNullOrEmpty(reference)) return null;
        var slash = reference.IndexOf('/');
        return slash > 0 ? reference[..slash] : null;
    }

    private static string? ExtractResourceId(string? reference)
    {
        if (string.IsNullOrEmpty(reference)) return null;
        var slash = reference.IndexOf('/');
        return slash > 0 && slash + 1 < reference.Length ? reference[(slash + 1)..] : null;
    }

    private static string? ExtractAgentId(AuditEvent auditEvent)
    {
        var agent = auditEvent.Agent.Count > 0 ? auditEvent.Agent[0] : null;
        return ExtractResourceId(agent?.Who?.Reference);
    }
}
