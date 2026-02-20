using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Persistence;

public sealed class IntegrationEventOutboxConfiguration : IEntityTypeConfiguration<IntegrationEventOutboxEntity>
{
    public void Configure(EntityTypeBuilder<IntegrationEventOutboxEntity> e)
    {
        _ = e.ToTable("IntegrationEventOutbox");
        _ = e.HasKey(x => x.Id);
        _ = e.Property(x => x.Id).HasConversion(v => v.ToString(), v => Ulid.Parse(v));
        _ = e.Property(x => x.EventType).HasMaxLength(500).IsRequired();
        _ = e.Property(x => x.Payload).HasColumnType("text").IsRequired();
        _ = e.Property(x => x.CreatedAtUtc).IsRequired();
        _ = e.Property(x => x.Error).HasMaxLength(2000);
        _ = e.HasIndex(x => x.CreatedAtUtc);
    }
}
