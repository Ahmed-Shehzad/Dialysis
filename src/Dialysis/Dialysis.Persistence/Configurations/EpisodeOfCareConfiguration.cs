using System.Text.Json;

using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class EpisodeOfCareConfiguration : IEntityTypeConfiguration<EpisodeOfCare>
{
    public void Configure(EntityTypeBuilder<EpisodeOfCare> builder)
    {
        builder.ToTable("episode_of_care");

        builder.Ignore(e => e.DeletedAtUtc);
        builder.Ignore(e => e.IsDeleted);

        builder.HasKey(e => new { e.TenantId, e.Id });

        builder.Property(e => e.Id)
            .HasConversion(ulid => ulid.ToString(), s => Ulid.Parse(s))
            .HasMaxLength(64);

        builder.Property(e => e.TenantId)
            .HasConversion(t => t.Value, v => new TenantId(v))
            .HasMaxLength(64);

        builder.Property(e => e.PatientId)
            .HasConversion(p => p.Value, v => new PatientId(v))
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.PeriodStart);
        builder.Property(e => e.PeriodEnd);
        builder.Property(e => e.Description).HasMaxLength(512);
        builder.Property(e => e.DiagnosisConditionIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<List<string>>(v) ?? new List<string>());
        builder.Property(e => e.CreatedAtUtc).HasColumnName("CreatedAt");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("UpdatedAt");

        builder.HasIndex(e => new { e.TenantId, e.PatientId });
        builder.HasIndex(e => new { e.TenantId, e.Status });
    }
}
