using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore;

public sealed class SubscriptionRecordConfiguration : IEntityTypeConfiguration<SubscriptionRecord>
{
    public const string SchemaName = "fhir_subscriptions";

    public void Configure(EntityTypeBuilder<SubscriptionRecord> builder)
    {
        builder.ToTable("subscriptions", SchemaName);
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasMaxLength(128);
        builder.Property(s => s.TopicUrl).HasMaxLength(512).IsRequired();
        builder.Property(s => s.ChannelType).HasConversion<string>().HasMaxLength(32);
        builder.Property(s => s.ChannelEndpoint).HasMaxLength(2048).IsRequired();
        builder.Property(s => s.ChannelHeader).HasMaxLength(2048);
        builder.Property(s => s.FilterParametersJson).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(s => new { s.TopicUrl, s.Status });
    }
}

public sealed class NotificationOutboxRecordConfiguration : IEntityTypeConfiguration<NotificationOutboxRecord>
{
    public void Configure(EntityTypeBuilder<NotificationOutboxRecord> builder)
    {
        builder.ToTable("notification_outbox", SubscriptionRecordConfiguration.SchemaName);
        builder.HasKey(r => r.Id);
        builder.Property(r => r.SubscriptionId).HasMaxLength(128).IsRequired();
        builder.Property(r => r.PayloadJson).IsRequired();
        builder.Property(r => r.LastError).HasMaxLength(1024);
        builder.HasIndex(r => new { r.SubscriptionId, r.DeliveredAt });
    }
}
