using Dialysis.BuildingBlocks.Transponder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

/// <summary>
/// Polls unprocessed outbox rows and publishes them through <see cref="ITransponderBus"/>. Run a single instance per logical database or coordinate externally.
/// </summary>
public sealed class TransponderOutboxRelayHostedService<TContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<TransponderOutboxRelayOptions> options,
    ILogger<TransponderOutboxRelayHostedService<TContext>> logger) : BackgroundService
    where TContext : TransponderPersistenceDbContextBase
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedAny = false;
                await using (var scope = scopeFactory.CreateAsyncScope())
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
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(
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
                logger.LogError(ex, "Transponder outbox relay loop error; backing off.");
                try
                {
                    await Task.Delay(options.Value.IdlePollInterval, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
