using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("alerts");

        builder.HasKey(a => new { a.TenantId, a.Id });

        builder.Property(a => a.Id)
            .HasConversion(ulid => ulid.ToString(), s => Ulid.Parse(s))
            .HasMaxLength(64);

        builder.Property(a => a.TenantId)
            .HasConversion(t => t.Value, v => new TenantId(v))
            .HasMaxLength(64);

        builder.Property(a => a.PatientId)
            .HasConversion(p => p.Value, v => new PatientId(v))
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(a => a.ObservationId)
            .HasConversion(o => o == null ? null : o.Value, v => string.IsNullOrEmpty(v) ? null : new ObservationId(v))
            .HasMaxLength(64);

        builder.Property(a => a.Severity).HasMaxLength(32);
        builder.Property(a => a.Message).HasMaxLength(512);
        builder.Property(a => a.Status).HasConversion<int>();
        builder.Property(a => a.AcknowledgedAtUtc);
        builder.Property(a => a.AcknowledgedBy).HasMaxLength(128);

        builder.Property(a => a.CreatedAtUtc).HasColumnName("CreatedAt");
        builder.Property(a => a.UpdatedAtUtc).HasColumnName("UpdatedAt");

        builder.HasIndex(a => new { a.TenantId, a.PatientId, a.Status });
    }
}
