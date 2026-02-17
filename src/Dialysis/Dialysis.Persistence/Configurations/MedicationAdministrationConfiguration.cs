using Dialysis.Domain.Aggregates;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class MedicationAdministrationConfiguration : IEntityTypeConfiguration<MedicationAdministration>
{
    public void Configure(EntityTypeBuilder<MedicationAdministration> builder)
    {
        builder.ToTable("medication_administrations");

        builder.Ignore(o => o.DomainEvents);
        builder.Ignore(o => o.IntegrationEvents);
        builder.Ignore(nameof(MedicationAdministration.UpdatedAtUtc));
        builder.Ignore(nameof(MedicationAdministration.DeletedAtUtc));
        builder.Ignore(nameof(MedicationAdministration.IsDeleted));

        builder.HasKey(o => new { o.TenantId, o.Id });

        builder.Property(o => o.Id)
            .HasConversion(ulid => ulid.ToString(), s => Ulid.Parse(s))
            .HasMaxLength(64);

        builder.Property(o => o.TenantId)
            .HasConversion(t => t.Value, v => new TenantId(v))
            .HasMaxLength(64);

        builder.Property(o => o.PatientId)
            .HasConversion(p => p.Value, v => new PatientId(v))
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(o => o.SessionId).HasMaxLength(64);
        builder.Property(o => o.MedicationCode).IsRequired().HasMaxLength(64);
        builder.Property(o => o.MedicationDisplay).HasMaxLength(256);
        builder.Property(o => o.DoseQuantity).HasMaxLength(64);
        builder.Property(o => o.DoseUnit).HasMaxLength(32);
        builder.Property(o => o.Route).HasMaxLength(32);
        builder.Property(o => o.Status).HasMaxLength(32);
        builder.Property(o => o.ReasonText).HasMaxLength(512);
        builder.Property(o => o.PerformerId).HasMaxLength(64);

        builder.Property(o => o.CreatedAtUtc).HasColumnName("CreatedAt");
        builder.Property(o => o.EffectiveAt).IsRequired();

        builder.HasIndex(o => new { o.TenantId, o.PatientId, o.EffectiveAt });
        builder.HasIndex(o => new { o.TenantId, o.SessionId });
    }
}
