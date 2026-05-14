namespace Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;

/// <summary>
/// EF-Core-mapped projection of a FHIR <c>AuditEvent</c>. Stored as the serialized FHIR JSON in
/// <see cref="ResourceJson"/> with a few denormalized columns for indexing/retention queries.
/// </summary>
public sealed class AuditEventRecord
{
    public Guid Id { get; set; }

    public required DateTimeOffset RecordedAt { get; set; }

    public required string ModuleSlug { get; set; }

    public required string Subtype { get; set; }

    public string? AgentId { get; set; }

    public string? ResourceType { get; set; }

    public string? ResourceId { get; set; }

    public required string Outcome { get; set; }

    public required string ResourceJson { get; set; }
}
