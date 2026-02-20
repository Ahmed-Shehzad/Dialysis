using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Persistence;

public sealed class IntegrationEventInboxConfiguration : IEntityTypeConfiguration<IntegrationEventInboxEntity>
{
    public void Configure(EntityTypeBuilder<IntegrationEventInboxEntity> builder)
    {
        _ = builder.ToTable("IntegrationEventInbox");
        _ = builder.HasKey(x => x.MessageId);
        _ = builder.Property(x => x.MessageId).HasMaxLength(256).IsRequired();
        _ = builder.Property(x => x.ProcessedAtUtc).IsRequired();
        _ = builder.Property(x => x.TenantId).HasMaxLength(64);
        _ = builder.Property(x => x.EventType).HasMaxLength(500);
        _ = builder.HasIndex(x => x.ProcessedAtUtc);
    }
}
