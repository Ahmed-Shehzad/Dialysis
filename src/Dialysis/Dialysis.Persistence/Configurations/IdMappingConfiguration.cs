using Dialysis.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.Persistence.Configurations;

public sealed class IdMappingConfiguration : IEntityTypeConfiguration<IdMapping>
{
    public void Configure(EntityTypeBuilder<IdMapping> builder)
    {
        builder.ToTable("id_mappings");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasConversion(ulid => ulid.ToString(), s => Ulid.Parse(s))
            .HasMaxLength(64);

        builder.Property(m => m.TenantId).IsRequired().HasMaxLength(64);
        builder.Property(m => m.ResourceType).IsRequired().HasMaxLength(64);
        builder.Property(m => m.LocalId).IsRequired().HasMaxLength(128);
        builder.Property(m => m.ExternalSystem).IsRequired().HasMaxLength(64);
        builder.Property(m => m.ExternalId).IsRequired().HasMaxLength(256);
        builder.Property(m => m.CreatedAtUtc).HasColumnName("CreatedAt");

        builder.HasIndex(m => new { m.TenantId, m.ResourceType, m.LocalId, m.ExternalSystem }).IsUnique();
        builder.HasIndex(m => new { m.TenantId, m.ExternalSystem, m.ExternalId });
    }
}
