using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.EntityFramework.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Base DbContext for Transponder inbox/outbox persistence.
/// </summary>
public class TransponderDbContext : DbContext
{
    private readonly IEntityFrameworkStorageOptions _storageOptions;

    public TransponderDbContext(DbContextOptions options, IEntityFrameworkStorageOptions storageOptions)
        : base(options)
    {
        _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
    }

    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    public DbSet<InboxStateEntity> InboxStates => Set<InboxStateEntity>();

    public DbSet<ScheduledMessageEntity> ScheduledMessages => Set<ScheduledMessageEntity>();

    public DbSet<SagaStateEntity> SagaStates => Set<SagaStateEntity>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        _ = configurationBuilder.Properties<Ulid>()
            .HaveConversion<UlidToStringConverter>()
            .HaveMaxLength(26)
            .HaveColumnType("character(26)");
        _ = configurationBuilder.Properties<Ulid?>()
            .HaveConversion<NullableUlidToStringConverter>()
            .HaveMaxLength(26)
            .HaveColumnType("character(26)");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var schema = _storageOptions.Schema;

        _ = modelBuilder.Entity<OutboxMessageEntity>(entity =>
        {
            _ = entity.ToTable(_storageOptions.OutboxTableName, schema);
            _ = entity.HasKey(message => message.MessageId);
            _ = entity.Property(message => message.MessageId).ValueGeneratedNever();
            _ = entity.Property(message => message.Body).IsRequired();
            _ = entity.Property(message => message.Headers);
            _ = entity.Property(message => message.SourceAddress).HasMaxLength(2048);
            _ = entity.Property(message => message.DestinationAddress).HasMaxLength(2048);
        });

        _ = modelBuilder.Entity<InboxStateEntity>(entity =>
        {
            _ = entity.ToTable(_storageOptions.InboxTableName, schema);
            _ = entity.HasKey(state => new { state.MessageId, state.ConsumerId });
            _ = entity.Property(state => state.MessageId).ValueGeneratedNever();
            _ = entity.Property(state => state.ConsumerId).HasMaxLength(200);
        });

        _ = modelBuilder.Entity<ScheduledMessageEntity>(entity =>
        {
            _ = entity.ToTable(_storageOptions.ScheduledMessagesTableName, schema);
            _ = entity.HasKey(message => message.TokenId);
            _ = entity.Property(message => message.TokenId).ValueGeneratedNever();
            _ = entity.Property(message => message.MessageType).HasMaxLength(500).IsRequired();
            _ = entity.Property(message => message.Body).IsRequired();
            _ = entity.Property(message => message.Headers);
            _ = entity.HasIndex(message => new { message.ScheduledTime, message.DispatchedTime });
        });

        _ = modelBuilder.Entity<SagaStateEntity>(entity =>
        {
            _ = entity.ToTable(_storageOptions.SagaStatesTableName, schema);
            _ = entity.HasKey(state => new { state.CorrelationId, state.StateType });
            _ = entity.Property(state => state.CorrelationId).ValueGeneratedNever();
            _ = entity.Property(state => state.StateType).HasMaxLength(500).IsRequired();
            _ = entity.Property(state => state.StateData).IsRequired();
            _ = entity.HasIndex(state => state.ConversationId);
        });
    }
}
