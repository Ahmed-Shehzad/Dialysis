using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class ConditionConfiguration : IEntityTypeConfiguration<Condition>
{
    public void Configure(EntityTypeBuilder<Condition> builder)
    {
        builder.ToTable("conditions");

        builder.Ignore(c => c.DeletedAtUtc);
        builder.Ignore(c => c.IsDeleted);

        builder.HasKey(c => new { c.TenantId, c.Id });

        builder.Property(c => c.Id)
            .HasConversion(ulid => ulid.ToString(), s => Ulid.Parse(s))
            .HasMaxLength(64);

        builder.Property(c => c.TenantId)
            .HasConversion(t => t.Value, v => new TenantId(v))
            .HasMaxLength(64);

        builder.Property(c => c.PatientId)
            .HasConversion(p => p.Value, v => new PatientId(v))
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(c => c.CodeSystem).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(64);
        builder.Property(c => c.Display).HasMaxLength(256);
        builder.Property(c => c.ClinicalStatus).IsRequired().HasMaxLength(32);
        builder.Property(c => c.VerificationStatus).IsRequired().HasMaxLength(32);
        builder.Property(c => c.OnsetDateTime);
        builder.Property(c => c.RecordedDate);
        builder.Property(c => c.CreatedAtUtc).HasColumnName("CreatedAt");
        builder.Property(c => c.UpdatedAtUtc).HasColumnName("UpdatedAt");

        builder.HasIndex(c => new { c.TenantId, c.PatientId });
        builder.HasIndex(c => new { c.TenantId, c.Code, c.CodeSystem });
    }
}
