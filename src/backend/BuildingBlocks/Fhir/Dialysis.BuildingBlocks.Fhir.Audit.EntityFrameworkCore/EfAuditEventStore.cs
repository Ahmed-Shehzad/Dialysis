using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;

/// <summary>
/// Persists <see cref="AuditEvent"/> resources as <see cref="AuditEventRecord"/> rows on the host
/// module's <typeparamref name="TDbContext"/>. Modules add
/// <see cref="AuditEventRecordConfiguration"/> to their <c>OnModelCreating</c> to enable this store.
/// </summary>
public sealed class EfAuditEventStore<TDbContext> : IAuditEventStore
    where TDbContext : DbContext
{
    private readonly TDbContext _db;
    /// <summary>
    /// Persists <see cref="AuditEvent"/> resources as <see cref="AuditEventRecord"/> rows on the host
    /// module's <typeparamref name="TDbContext"/>. Modules add
    /// <see cref="AuditEventRecordConfiguration"/> to their <c>OnModelCreating</c> to enable this store.
    /// </summary>
    public EfAuditEventStore(TDbContext db) => _db = db;
    // ToJson is CPU-only; calling it from a non-Async method keeps VSTHRD103 quiet.
    private static string SerializeFhirJson(AuditEvent auditEvent) => auditEvent.ToJson();

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
            ResourceJson = SerializeFhirJson(auditEvent),
        };

        _db.Set<AuditEventRecord>().Add(record);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
