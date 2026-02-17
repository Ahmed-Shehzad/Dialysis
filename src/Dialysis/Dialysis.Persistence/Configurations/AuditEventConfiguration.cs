using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");

        builder.HasKey(e => new { e.TenantId, e.Id });

        builder.Property(e => e.Id)
            .HasConversion(ulid => ulid.ToString(), s => Ulid.Parse(s))
            .HasMaxLength(64);

        builder.Property(e => e.TenantId)
            .HasConversion(t => t.Value, v => new TenantId(v))
            .HasMaxLength(64);

        builder.Property(e => e.Actor).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Action).IsRequired().HasMaxLength(64);
        builder.Property(e => e.ResourceType).IsRequired().HasMaxLength(64);
        builder.Property(e => e.ResourceId).HasMaxLength(128);
        builder.Property(e => e.PatientId).HasMaxLength(64);
        builder.Property(e => e.Details).HasMaxLength(2000);

        builder.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt");

        builder.HasIndex(e => new { e.TenantId, e.PatientId, e.CreatedAtUtc });
        builder.HasIndex(e => new { e.TenantId, e.ResourceType, e.CreatedAtUtc });
    }
}
