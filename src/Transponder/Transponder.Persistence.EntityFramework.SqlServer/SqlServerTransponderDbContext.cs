using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.EntityFramework.SqlServer.Abstractions;

namespace Transponder.Persistence.EntityFramework.SqlServer;

/// <summary>
/// SQL Server DbContext for Transponder inbox/outbox persistence.
/// </summary>
public sealed class SqlServerTransponderDbContext : TransponderDbContext
{
    public SqlServerTransponderDbContext(
        DbContextOptions<SqlServerTransponderDbContext> options,
        ISqlServerStorageOptions storageOptions)
        : base(options, storageOptions)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.Entity<OutboxMessageEntity>(entity =>
        {
            _ = entity.Property(message => message.Body).HasColumnType("varbinary(max)");
            _ = entity.Property(message => message.Headers).HasColumnType("nvarchar(max)");
            _ = entity.Property(message => message.SourceAddress).HasColumnType("nvarchar(2048)");
            _ = entity.Property(message => message.DestinationAddress).HasColumnType("nvarchar(2048)");
        });

        _ = modelBuilder.Entity<ScheduledMessageEntity>(entity =>
        {
            _ = entity.Property(message => message.Body).HasColumnType("varbinary(max)");
            _ = entity.Property(message => message.Headers).HasColumnType("nvarchar(max)");
            _ = entity.Property(message => message.MessageType).HasColumnType("nvarchar(500)");
        });

        _ = modelBuilder.Entity<SagaStateEntity>(entity =>
        {
            _ = entity.Property(state => state.StateType).HasColumnType("nvarchar(500)");
            _ = entity.Property(state => state.StateData).HasColumnType("nvarchar(max)");
        });
    }
}
