using Dialysis.Domain.Aggregates;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.Ignore(s => s.DomainEvents);
        builder.Ignore(s => s.IntegrationEvents);

        builder.HasKey(s => new { s.TenantId, s.Id });

        builder.Property(s => s.Id)
            .HasConversion(ulid => ulid.ToString(), s => Ulid.Parse(s))
            .HasMaxLength(64);

        builder.Property(s => s.TenantId)
            .HasConversion(t => t.Value, v => new TenantId(v))
            .HasMaxLength(64);

        builder.Property(s => s.PatientId)
            .HasConversion(p => p.Value, v => new PatientId(v))
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(s => s.StartedAt).IsRequired();
        builder.Property(s => s.EndedAt);
        builder.Property(s => s.AccessSite).HasMaxLength(64);
        builder.Property(s => s.EncounterId).HasMaxLength(128);
        builder.Property(s => s.UfRemovedKg).HasPrecision(10, 2);
        builder.Property(s => s.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc).HasColumnName("CreatedAt");
        builder.Property(s => s.UpdatedAtUtc).HasColumnName("UpdatedAt");

        builder.HasIndex(s => new { s.TenantId, s.PatientId, s.StartedAt });
    }
}
