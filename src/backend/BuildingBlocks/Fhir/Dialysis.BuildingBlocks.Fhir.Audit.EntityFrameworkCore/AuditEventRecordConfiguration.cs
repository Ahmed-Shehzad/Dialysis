using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;

/// <summary>
/// EF Core configuration for <see cref="AuditEventRecord"/>. Apply on each module's
/// <c>OnModelCreating</c> override; the schema name <c>fhir_audit</c> is honored as a sibling of
/// Transponder's <c>transponder</c> schema.
/// </summary>
public sealed class AuditEventRecordConfiguration : IEntityTypeConfiguration<AuditEventRecord>
{
    public const string SchemaName = "fhir_audit";

    public void Configure(EntityTypeBuilder<AuditEventRecord> builder)
    {
        builder.ToTable("audit_events", SchemaName);
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RecordedAt).IsRequired();
        builder.Property(r => r.ModuleSlug).HasMaxLength(32).IsRequired();
        builder.Property(r => r.Subtype).HasMaxLength(64).IsRequired();
        builder.Property(r => r.AgentId).HasMaxLength(128);
        builder.Property(r => r.ResourceType).HasMaxLength(64);
        builder.Property(r => r.ResourceId).HasMaxLength(128);
        builder.Property(r => r.Outcome).HasMaxLength(8).IsRequired();
        builder.Property(r => r.ResourceJson).IsRequired();
        builder.HasIndex(r => r.RecordedAt);
        builder.HasIndex(r => new { r.ResourceType, r.ResourceId });
    }
}
