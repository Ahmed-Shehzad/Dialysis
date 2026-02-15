using Transponder.Persistence.EntityFramework.Abstractions;

namespace Transponder.Persistence.EntityFramework.PostgreSql.Abstractions;

/// <summary>
/// Provides PostgreSQL specific settings for Entity Framework persistence.
/// </summary>
public interface IPostgreSqlStorageOptions : IEntityFrameworkStorageOptions
{
    /// <summary>
    /// Gets whether advisory locks should be used for inbox/outbox operations.
    /// </summary>
    bool UseAdvisoryLocks { get; }

    /// <summary>
    /// Gets whether SKIP LOCKED should be used when reading outbox rows.
    /// </summary>
    bool UseSkipLocked { get; }
}
