using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// Polls unprocessed outbox rows and publishes them through <see cref="ITransponderBus"/>. Run a single instance per logical database or coordinate externally.
/// </summary>
public sealed class TransponderOutboxRelayHostedService<TContext> : BackgroundService
    where TContext : TransponderPersistenceDbContextBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<TransponderOutboxRelayOptions> _options;
    private readonly ILogger<TransponderOutboxRelayHostedService<TContext>> _logger;
    /// <summary>
    /// Polls unprocessed outbox rows and publishes them through <see cref="ITransponderBus"/>. Run a single instance per logical database or coordinate externally.
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
