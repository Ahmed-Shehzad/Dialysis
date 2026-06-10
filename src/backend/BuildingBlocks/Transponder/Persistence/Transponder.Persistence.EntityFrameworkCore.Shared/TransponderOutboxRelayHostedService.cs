using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// Polls unprocessed outbox rows and publishes them through <see cref="ITransponderBus"/>.
/// Multi-replica safe on PostgreSQL: each polling tick first takes a per-database session
/// advisory lock (<see cref="AdvisoryLockKey"/>), so when a module host scales horizontally
/// only one replica relays at a time — the others skip the tick and retry after the idle
/// interval. Different modules never contend (the lock is scoped to the module's own
/// database). On non-PostgreSQL providers the lock is skipped and consumers' inbox
/// deduplication remains the at-least-once safety net.
/// </summary>
public sealed class TransponderOutboxRelayHostedService<TContext> : BackgroundService
    where TContext : TransponderPersistenceDbContextBase
{
    /// <summary>
    /// Arbitrary-but-stable key for the per-database relay advisory lock. Session-scoped
    /// (<c>pg_try_advisory_lock</c>), explicitly released in <c>finally</c> — Npgsql's pooled
    /// connection reset does not release advisory locks, so relying on close would leak the
    /// lock into the pool. A crashed replica's lock is released by the server when its
    /// connection drops.
    /// </summary>
    public const long AdvisoryLockKey = 730_415_523_891_217;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<TransponderOutboxRelayOptions> _options;
    private readonly ILogger<TransponderOutboxRelayHostedService<TContext>> _logger;
    /// <summary>
    /// Polls unprocessed outbox rows and publishes them through <see cref="ITransponderBus"/>.
    /// Multi-replica safe on PostgreSQL via a per-database advisory lock; see the class docs.
    /// </summary>
    public TransponderOutboxRelayHostedService(IServiceScopeFactory scopeFactory,
        IOptions<TransponderOutboxRelayOptions> options,
        ILogger<TransponderOutboxRelayHostedService<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.Value;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedAny = false;
                await using (var scope = _scopeFactory.CreateAsyncScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<TContext>();
                    var bus = scope.ServiceProvider.GetRequiredService<ITransponderBus>();
                    var serializer = scope.ServiceProvider.GetRequiredService<IMessageSerializer>();

                    // Horizontal-scaling guard: only one replica relays this database per tick.
                    // The session advisory lock must live on a connection we hold open for the
                    // whole tick, and must be explicitly unlocked (see AdvisoryLockKey docs).
                    var usesAdvisoryLock = db.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true;
                    if (usesAdvisoryLock)
                    {
                        await db.Database.OpenConnectionAsync(stoppingToken).ConfigureAwait(false);
                        var acquired = await db.Database
                            .SqlQueryRaw<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"")
                            .SingleAsync(stoppingToken)
                            .ConfigureAwait(false);
                        if (!acquired)
                        {
                            await db.Database.CloseConnectionAsync().ConfigureAwait(false);
                            await Task.Delay(opts.IdlePollInterval, stoppingToken).ConfigureAwait(false);
                            continue;
                        }
                    }

                    try
                    {
                        var batch = await db.OutboxMessages
                            .Where(o => o.ProcessedAtUtc == null)
                            .OrderBy(o => o.CreatedAtUtc)
                            .Take(opts.BatchSize)
                            .ToListAsync(stoppingToken)
                            .ConfigureAwait(false);

                        // Lag gauge from the batch head (ordered by CreatedAtUtc) — zero extra queries.
                        TransponderOutboxMetrics.RecordOldestPendingAge(
                            typeof(TContext).Name,
                            batch.Count == 0 ? TimeSpan.Zero : DateTime.UtcNow - batch[0].CreatedAtUtc);

                        foreach (var row in batch)
                        {
                            try
                            {
                                await TransponderOutboxRelayPublish
                                    .PublishRowAsync(bus, serializer, row, stoppingToken)
                                    .ConfigureAwait(false);
                                row.ProcessedAtUtc = DateTime.UtcNow;
                                await db.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
                                processedAny = true;
                                TransponderOutboxMetrics.RecordPublished(typeof(TContext).Name);
                            }
                            catch (Exception ex)
                            {
                                TransponderOutboxMetrics.RecordFailure(typeof(TContext).Name);
                                _logger.LogError(
                                    ex,
                                    "Transponder outbox relay failed for row {OutboxId}; will retry on next poll.",
                                    row.Id);
                                break;
                            }
                        }
                    }
                    finally
                    {
                        if (usesAdvisoryLock)
                        {
                            try
                            {
                                _ = await db.Database
                                    .SqlQueryRaw<bool>($"SELECT pg_advisory_unlock({AdvisoryLockKey}) AS \"Value\"")
                                    .SingleAsync(CancellationToken.None)
                                    .ConfigureAwait(false);
                            }
                            finally
                            {
                                await db.Database.CloseConnectionAsync().ConfigureAwait(false);
                            }
                        }
                    }
                }

                var delay = processedAny ? TimeSpan.Zero : opts.IdlePollInterval;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transponder outbox relay loop error; backing off.");
                try
                {
                    await Task.Delay(_options.Value.IdlePollInterval, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
