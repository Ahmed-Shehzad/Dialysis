using Microsoft.EntityFrameworkCore;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

public static class TransponderPersistenceModelConfiguration
{
    public static void Configure(ModelBuilder modelBuilder, string schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        modelBuilder.HasDefaultSchema(schema);

        modelBuilder.Entity<TransponderOutboxMessageEntity>(e =>
        {
            e.ToTable("OutboxMessages");
            e.HasKey(o => o.Id);
            e.Property(o => o.AssemblyQualifiedEventType).HasMaxLength(512).IsRequired();
            e.Property(o => o.PayloadJson).IsRequired();
            e.Property(o => o.CreatedAtUtc).IsRequired();
            e.Property(o => o.W3CTraceParent).HasMaxLength(128);
            e.Property(o => o.CorrelationId).HasMaxLength(128);
            e.HasIndex(o => o.ProcessedAtUtc);
        });

        modelBuilder.Entity<TransponderInboxMessageEntity>(e =>
        {
            e.ToTable("InboxMessages");
            e.HasKey(o => o.Id);
            e.Property(o => o.DeduplicationKey).HasMaxLength(256).IsRequired();
            e.Property(o => o.RoutingKey).HasMaxLength(512).IsRequired();
            e.Property(o => o.CreatedAtUtc).IsRequired();
            e.HasIndex(o => o.DeduplicationKey).IsUnique();
            e.HasIndex(o => o.CompletedAtUtc);
        });

        modelBuilder.Entity<TransponderSagaInstanceEntity>(e =>
        {
            e.ToTable("SagaInstances");
            e.HasKey(o => o.Id);
            e.Property(o => o.SagaKind).HasMaxLength(512).IsRequired();
            e.Property(o => o.InstanceKey).HasMaxLength(512).IsRequired();
            e.Property(o => o.StateName).HasMaxLength(256).IsRequired();
            e.Property(o => o.StateJson);
            e.Property(o => o.Version).IsConcurrencyToken();
            e.Property(o => o.IsCompleted).IsRequired();
            e.Property(o => o.UpdatedAtUtc).IsRequired();
            e.HasIndex(o => new { o.SagaKind, o.InstanceKey }).IsUnique();
        });
    }
}
