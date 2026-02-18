using Transponder.Persistence.EntityFramework.Abstractions;

namespace Transponder.Persistence.EntityFramework.SqlServer.Abstractions;

/// <summary>
/// Provides SQL Server specific settings for Entity Framework persistence.
/// </summary>
public interface ISqlServerStorageOptions : IEntityFrameworkStorageOptions
{
    /// <summary>
    /// Gets whether snapshot isolation should be used for inbox/outbox operations.
    /// </summary>
    bool UseSnapshotIsolation { get; }

    /// <summary>
    /// Gets a custom lock hint to apply when reading outbox rows, if any.
    /// </summary>
    string? OutboxLockHint { get; }
}
