using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("service_requests");

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

        builder.Property(c => c.Code).IsRequired().HasMaxLength(64);
        builder.Property(c => c.Display).HasMaxLength(256);
        builder.Property(c => c.Status).IsRequired().HasMaxLength(32);
        builder.Property(c => c.Intent).HasMaxLength(32);
        builder.Property(c => c.EncounterId).HasMaxLength(64);
        builder.Property(c => c.SessionId).HasMaxLength(64);
        builder.Property(c => c.ReasonText).HasMaxLength(512);
        builder.Property(c => c.RequesterId).HasMaxLength(64);
        builder.Property(c => c.Frequency).HasMaxLength(128);
        builder.Property(c => c.Category).HasMaxLength(64);

        builder.Property(c => c.CreatedAtUtc).HasColumnName("CreatedAt");
        builder.Property(c => c.UpdatedAtUtc).HasColumnName("UpdatedAt");

        builder.HasIndex(c => new { c.TenantId, c.PatientId });
        builder.HasIndex(c => new { c.TenantId, c.Status });
    }
}
