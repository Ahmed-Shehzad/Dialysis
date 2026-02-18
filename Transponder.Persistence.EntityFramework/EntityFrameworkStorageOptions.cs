using Transponder.Persistence.EntityFramework.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Default Entity Framework storage options.
/// </summary>
public class EntityFrameworkStorageOptions : IEntityFrameworkStorageOptions
{
    public EntityFrameworkStorageOptions(
        string? schema = null,
        string outboxTableName = "OutboxMessages",
        string inboxTableName = "InboxStates",
        string scheduledMessagesTableName = "ScheduledMessages",
        string sagaStatesTableName = "SagaStates")
    {
        if (string.IsNullOrWhiteSpace(outboxTableName)) throw new ArgumentException("Outbox table name must be provided.", nameof(outboxTableName));

        if (string.IsNullOrWhiteSpace(inboxTableName)) throw new ArgumentException("Inbox table name must be provided.", nameof(inboxTableName));

        if (string.IsNullOrWhiteSpace(scheduledMessagesTableName))
            throw new ArgumentException(
                "Scheduled messages table name must be provided.",
                nameof(scheduledMessagesTableName));

        if (string.IsNullOrWhiteSpace(sagaStatesTableName)) throw new ArgumentException("Saga states table name must be provided.", nameof(sagaStatesTableName));

        Schema = string.IsNullOrWhiteSpace(schema) ? null : schema;
        OutboxTableName = outboxTableName;
        InboxTableName = inboxTableName;
        ScheduledMessagesTableName = scheduledMessagesTableName;
        SagaStatesTableName = sagaStatesTableName;
    }

    /// <inheritdoc />
    public string? Schema { get; }

    /// <inheritdoc />
    public string OutboxTableName { get; }

    /// <inheritdoc />
    public string InboxTableName { get; }

    /// <inheritdoc />
    public string ScheduledMessagesTableName { get; }

    /// <inheritdoc />
    public string SagaStatesTableName { get; }
}
