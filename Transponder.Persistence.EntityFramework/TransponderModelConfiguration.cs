using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.EntityFramework.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Extension to apply Transponder outbox/inbox/scheduler/saga model configuration to any DbContext.
/// Use when storing Transponder tables in the application database for same-transaction atomicity.
/// </summary>
public static class TransponderModelConfiguration
{
    /// <summary>
    /// Applies Ulid conversion conventions required for Transponder entities.
    /// Call from <c>ConfigureConventions</c> when adding Transponder tables to a DbContext.
    /// </summary>
    public static ModelConfigurationBuilder ApplyTransponderUlidConventions(
        this ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        _ = configurationBuilder.Properties<Ulid>()
            .HaveConversion<UlidToStringConverter>()
            .HaveMaxLength(26)
            .HaveColumnType("character(26)");
        _ = configurationBuilder.Properties<Ulid?>()
            .HaveConversion<NullableUlidToStringConverter>()
            .HaveMaxLength(26)
            .HaveColumnType("character(26)");

        return configurationBuilder;
    }
    /// <summary>
    /// Applies Transponder entity configuration (OutboxMessages, InboxStates, ScheduledMessages, SagaStates)
    /// to the given model builder. Uses default table names when <paramref name="storageOptions"/> is null.
    /// </summary>
    public static ModelBuilder ApplyTransponderModel(
        this ModelBuilder modelBuilder,
        IEntityFrameworkStorageOptions? storageOptions = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        storageOptions ??= new EntityFrameworkStorageOptions();

        string? schema = storageOptions.Schema;

        _ = modelBuilder.Entity<OutboxMessageRecord>(entity =>
        {
            _ = entity.ToTable(storageOptions.OutboxTableName, schema);
            _ = entity.HasKey(message => message.MessageId);
            _ = entity.Property(message => message.MessageId).ValueGeneratedNever();
            _ = entity.Property(message => message.Body).IsRequired();
            _ = entity.Property(message => message.Headers);
            _ = entity.Property(message => message.SourceAddress).HasMaxLength(2048);
            _ = entity.Property(message => message.DestinationAddress).HasMaxLength(2048);
        });

        _ = modelBuilder.Entity<InboxStateRecord>(entity =>
        {
            _ = entity.ToTable(storageOptions.InboxTableName, schema);
            _ = entity.HasKey(state => new { state.MessageId, state.ConsumerId });
            _ = entity.Property(state => state.MessageId).ValueGeneratedNever();
            _ = entity.Property(state => state.ConsumerId).HasMaxLength(200);
        });

        _ = modelBuilder.Entity<ScheduledMessageRecord>(entity =>
        {
            _ = entity.ToTable(storageOptions.ScheduledMessagesTableName, schema);
            _ = entity.HasKey(message => message.TokenId);
            _ = entity.Property(message => message.TokenId).ValueGeneratedNever();
            _ = entity.Property(message => message.MessageType).HasMaxLength(500).IsRequired();
            _ = entity.Property(message => message.Body).IsRequired();
            _ = entity.Property(message => message.Headers);
            _ = entity.HasIndex(message => new { message.ScheduledTime, message.DispatchedTime });
        });

        _ = modelBuilder.Entity<SagaStateRecord>(entity =>
        {
            _ = entity.ToTable(storageOptions.SagaStatesTableName, schema);
            _ = entity.HasKey(state => new { state.CorrelationId, state.StateType });
            _ = entity.Property(state => state.CorrelationId).ValueGeneratedNever();
            _ = entity.Property(state => state.StateType).HasMaxLength(500).IsRequired();
            _ = entity.Property(state => state.StateData).IsRequired();
            _ = entity.HasIndex(state => state.ConversationId);
        });

        return modelBuilder;
    }

    /// <summary>
    /// Applies PostgreSQL-specific column types for Transponder entities.
    /// Call after <see cref="ApplyTransponderModel"/> when using Npgsql.
    /// </summary>
    public static ModelBuilder ApplyPostgreSqlTransponderTypes(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        _ = modelBuilder.Entity<OutboxMessageRecord>(entity =>
        {
            _ = entity.Property(message => message.Body).HasColumnType("bytea");
            _ = entity.Property(message => message.Headers).HasColumnType("jsonb");
            _ = entity.Property(message => message.SourceAddress).HasColumnType("text");
            _ = entity.Property(message => message.DestinationAddress).HasColumnType("text");
        });

        _ = modelBuilder.Entity<ScheduledMessageRecord>(entity =>
        {
            _ = entity.Property(message => message.Body).HasColumnType("bytea");
            _ = entity.Property(message => message.Headers).HasColumnType("jsonb");
            _ = entity.Property(message => message.MessageType).HasColumnType("text");
        });

        _ = modelBuilder.Entity<SagaStateRecord>(entity =>
        {
            _ = entity.Property(state => state.StateType).HasColumnType("text");
            _ = entity.Property(state => state.StateData).HasColumnType("jsonb");
        });

        return modelBuilder;
    }
}
