using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.EntityFramework.PostgreSql.Abstractions;

namespace Transponder.Persistence.EntityFramework.PostgreSql;

/// <summary>
/// PostgreSQL DbContext for Transponder inbox/outbox persistence.
/// </summary>
public sealed class PostgreSqlTransponderDbContext : TransponderDbContext
{
    public PostgreSqlTransponderDbContext(
        DbContextOptions<PostgreSqlTransponderDbContext> options,
        IPostgreSqlStorageOptions storageOptions)
        : base(options, storageOptions)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.Entity<OutboxMessageEntity>(entity =>
        {
            _ = entity.Property(message => message.Body).HasColumnType("bytea");
            _ = entity.Property(message => message.Headers).HasColumnType("jsonb");
            _ = entity.Property(message => message.SourceAddress).HasColumnType("text");
            _ = entity.Property(message => message.DestinationAddress).HasColumnType("text");
        });

        _ = modelBuilder.Entity<ScheduledMessageEntity>(entity =>
        {
            _ = entity.Property(message => message.Body).HasColumnType("bytea");
            _ = entity.Property(message => message.Headers).HasColumnType("jsonb");
            _ = entity.Property(message => message.MessageType).HasColumnType("text");
        });

        _ = modelBuilder.Entity<SagaStateEntity>(entity =>
        {
            _ = entity.Property(state => state.StateType).HasColumnType("text");
            _ = entity.Property(state => state.StateData).HasColumnType("jsonb");
        });
    }
}
