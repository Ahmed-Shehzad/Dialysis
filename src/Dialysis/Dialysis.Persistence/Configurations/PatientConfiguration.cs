using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");

        builder.Ignore(p => p.UpdatedAtUtc);
        builder.Ignore(p => p.DeletedAtUtc);
        builder.Ignore(p => p.IsDeleted);
        builder.Ignore(p => p.Id);

        builder.HasKey(p => new { p.TenantId, p.LogicalId });

        builder.Property(p => p.TenantId)
            .HasConversion(t => t.Value, v => new TenantId(v))
            .HasMaxLength(64);

        builder.Property(p => p.LogicalId)
            .HasConversion(l => l.Value, v => new PatientId(v))
            .HasColumnName("Id")
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(p => p.FamilyName)
            .HasMaxLength(128);

        builder.Property(p => p.GivenNames)
            .HasMaxLength(256);

        builder.Property(p => p.CreatedAtUtc)
            .HasColumnName("CreatedAt");
    }
}
