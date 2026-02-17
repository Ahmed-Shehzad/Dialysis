using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class VascularAccessConfiguration : IEntityTypeConfiguration<VascularAccess>
{
    public void Configure(EntityTypeBuilder<VascularAccess> builder)
    {
        builder.ToTable("vascular_access");

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

        builder.Property(a => a.Type).HasConversion<int>().IsRequired();
        builder.Property(a => a.Side).HasMaxLength(16);
        builder.Property(a => a.PlacementDate);
        builder.Property(a => a.Status).HasConversion<int>().IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(1000);

        builder.Property(a => a.CreatedAtUtc).HasColumnName("CreatedAt");

        builder.HasIndex(a => new { a.TenantId, a.PatientId });
    }
}
