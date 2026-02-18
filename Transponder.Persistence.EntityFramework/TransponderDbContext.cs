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

    public DbSet<OutboxMessageRecord> OutboxMessages => Set<OutboxMessageRecord>();

    public DbSet<InboxStateRecord> InboxStates => Set<InboxStateRecord>();

    public DbSet<ScheduledMessageRecord> ScheduledMessages => Set<ScheduledMessageRecord>();

    public DbSet<SagaStateRecord> SagaStates => Set<SagaStateRecord>();

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

        string? schema = _storageOptions.Schema;

        _ = modelBuilder.Entity<OutboxMessageRecord>(entity =>
        {
            _ = entity.ToTable(_storageOptions.OutboxTableName, schema);
            _ = entity.HasKey(message => message.MessageId);
            _ = entity.Property(message => message.MessageId).ValueGeneratedNever();
            _ = entity.Property(message => message.Body).IsRequired();
            _ = entity.Property(message => message.Headers);
            _ = entity.Property(message => message.SourceAddress).HasMaxLength(2048);
            _ = entity.Property(message => message.DestinationAddress).HasMaxLength(2048);
        });

        _ = modelBuilder.Entity<InboxStateRecord>(entity =>
        {
            _ = entity.ToTable(_storageOptions.InboxTableName, schema);
            _ = entity.HasKey(state => new { state.MessageId, state.ConsumerId });
            _ = entity.Property(state => state.MessageId).ValueGeneratedNever();
            _ = entity.Property(state => state.ConsumerId).HasMaxLength(200);
        });

        _ = modelBuilder.Entity<ScheduledMessageRecord>(entity =>
        {
            _ = entity.ToTable(_storageOptions.ScheduledMessagesTableName, schema);
            _ = entity.HasKey(message => message.TokenId);
            _ = entity.Property(message => message.TokenId).ValueGeneratedNever();
            _ = entity.Property(message => message.MessageType).HasMaxLength(500).IsRequired();
            _ = entity.Property(message => message.Body).IsRequired();
            _ = entity.Property(message => message.Headers);
            _ = entity.HasIndex(message => new { message.ScheduledTime, message.DispatchedTime });
        });

        _ = modelBuilder.Entity<SagaStateRecord>(entity =>
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
