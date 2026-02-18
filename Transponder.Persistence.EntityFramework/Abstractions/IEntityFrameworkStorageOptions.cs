namespace Transponder.Persistence.EntityFramework.Abstractions;

/// <summary>
/// Provides table and schema settings for Entity Framework persistence.
/// </summary>
public interface IEntityFrameworkStorageOptions
{
    /// <summary>
    /// Gets the database schema name, if applicable.
    /// </summary>
    string? Schema { get; }

    /// <summary>
    /// Gets the outbox table name.
    /// </summary>
    string OutboxTableName { get; }

    /// <summary>
    /// Gets the inbox table name.
    /// </summary>
    string InboxTableName { get; }

    /// <summary>
    /// Gets the scheduled messages table name.
    /// </summary>
    string ScheduledMessagesTableName { get; }

    /// <summary>
    /// Gets the saga states table name.
    /// </summary>
    string SagaStatesTableName { get; }
}
