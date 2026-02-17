using Dialysis.Domain.Aggregates;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.ToTable("observations");

        builder.Ignore(o => o.DomainEvents);
        builder.Ignore(o => o.IntegrationEvents);
        builder.Ignore(nameof(Observation.UpdatedAtUtc));
        builder.Ignore(nameof(Observation.DeletedAtUtc));
        builder.Ignore(nameof(Observation.IsDeleted));

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

        builder.Property(o => o.LoincCode)
            .HasConversion(l => l.Value, v => new LoincCode(v))
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(o => o.Display)
            .HasMaxLength(256);

        builder.OwnsOne(o => o.Unit, unit =>
        {
            unit.Property(u => u.Value).HasColumnName("Unit").HasMaxLength(32);
            unit.Property(u => u.System).HasColumnName("UnitSystem").HasMaxLength(256);
        });

        builder.Property(o => o.NumericValue)
            .HasPrecision(18, 4);

        builder.Property(o => o.Effective)
            .HasConversion(e => e.Value, v => new ObservationEffective(v))
            .IsRequired();

        builder.Property(o => o.CreatedAtUtc)
            .HasColumnName("CreatedAt");

        builder.HasIndex(o => new { o.TenantId, o.PatientId, o.Effective });
    }
}
