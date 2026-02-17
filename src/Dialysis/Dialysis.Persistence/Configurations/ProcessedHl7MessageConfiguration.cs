using Dialysis.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class ProcessedHl7MessageConfiguration : IEntityTypeConfiguration<ProcessedHl7Message>
{
    public void Configure(EntityTypeBuilder<ProcessedHl7Message> builder)
    {
        builder.ToTable("processed_hl7_messages");

        builder.HasKey(p => new { p.TenantId, p.MessageControlId });

        builder.Property(p => p.TenantId)
            .HasConversion(t => t.Value, v => new SharedKernel.ValueObjects.TenantId(v))
            .HasMaxLength(64);

        builder.Property(p => p.MessageControlId).IsRequired().HasMaxLength(128);
        builder.Property(p => p.ProcessedAtUtc).HasColumnName("ProcessedAt");

        builder.HasIndex(p => new { p.TenantId, p.ProcessedAtUtc });
    }
}
