using Transponder.Persistence.EntityFramework.PostgreSql.Abstractions;

namespace Transponder.Persistence.EntityFramework.PostgreSql;

/// <summary>
/// Default PostgreSQL storage options.
/// </summary>
public sealed class PostgreSqlStorageOptions : EntityFrameworkStorageOptions, IPostgreSqlStorageOptions
{
    public PostgreSqlStorageOptions(
        string? schema = "public",
        string outboxTableName = "OutboxMessages",
        string inboxTableName = "InboxStates",
        string scheduledMessagesTableName = "ScheduledMessages",
        string sagaStatesTableName = "SagaStates",
        bool useAdvisoryLocks = true,
        bool useSkipLocked = true)
        : base(schema, outboxTableName, inboxTableName, scheduledMessagesTableName, sagaStatesTableName)
    {
        UseAdvisoryLocks = useAdvisoryLocks;
        UseSkipLocked = useSkipLocked;
    }

    /// <inheritdoc />
    public bool UseAdvisoryLocks { get; }

    /// <inheritdoc />
    public bool UseSkipLocked { get; }
}
