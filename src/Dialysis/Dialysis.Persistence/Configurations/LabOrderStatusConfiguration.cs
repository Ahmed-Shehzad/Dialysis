using Dialysis.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class LabOrderStatusConfiguration : IEntityTypeConfiguration<LabOrderStatus>
{
    public void Configure(EntityTypeBuilder<LabOrderStatus> builder)
    {
        builder.ToTable("lab_order_status");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasConversion(ulid => ulid.ToString(), s => Ulid.Parse(s))
            .HasMaxLength(64);

        builder.Property(l => l.TenantId)
            .HasConversion(t => t.Value, v => new SharedKernel.ValueObjects.TenantId(v))
            .HasMaxLength(64);

        builder.Property(l => l.PatientId)
            .HasConversion(p => p.Value, v => new SharedKernel.ValueObjects.PatientId(v))
            .HasMaxLength(64);

        builder.Property(l => l.PlacerOrderNumber).HasMaxLength(64);
        builder.Property(l => l.FillerOrderNumber).HasMaxLength(64);
        builder.Property(l => l.ServiceId).HasMaxLength(128);
        builder.Property(l => l.Status).HasMaxLength(16);
        builder.Property(l => l.LastUpdatedUtc).HasColumnName("LastUpdated");

        builder.HasIndex(l => new { l.TenantId, l.PlacerOrderNumber, l.FillerOrderNumber });
        builder.HasIndex(l => new { l.TenantId, l.PatientId });
    }
}
