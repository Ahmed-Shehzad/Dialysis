namespace Dialysis.BuildingBlocks.DataProtection.Erasure;

/// <summary>
/// Module-supplied job that purges aggregates past their retention window. Implementations
/// query their persistence layer for rows whose <c>CreatedAtUtc + RetentionDays &lt; now</c>,
/// soft-delete the aggregate, and delete the underlying blob bytes (where applicable).
///
/// The scheduler (a <see cref="Microsoft.Extensions.Hosting.BackgroundService"/> mirroring
/// <c>LtvUpgraderHostedService</c>) resolves a single registered implementation per host —
/// the modular monolith means one host runs the whole platform, so HIE Documents alone
/// satisfies the v1 surface. Additional eraser participants can later register multiple
/// implementations and the host can fan out across them.
/// </summary>
public interface IRetentionPurgeJob
{
    /// <summary>Runs one purge pass; returns the number of rows soft-deleted.</summary>
    Task<int> RunOnceAsync(CancellationToken cancellationToken);
}
