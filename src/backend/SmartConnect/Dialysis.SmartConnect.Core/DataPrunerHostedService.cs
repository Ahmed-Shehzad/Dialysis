using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect;

public sealed class DataPrunerHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<DataPrunerOptions> options,
    TimeProvider time,
    ILogger<DataPrunerHostedService> logger) : BackgroundService
{
    private readonly DataPrunerOptions _options = options.Value;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.Interval, time);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                var threshold = time.GetUtcNow() - _options.RetentionPeriod;
                await using var scope = scopeFactory.CreateAsyncScope();
                var ledger = scope.ServiceProvider.GetRequiredService<IMessageLedger>();
                var pruned = await ledger.PruneAsync(threshold, cancellationToken: stoppingToken).ConfigureAwait(false);
                if (pruned > 0)
                {
                    logger.LogInformation("Data pruner removed {Count} ledger entries older than {Threshold}.", pruned, threshold);
                }

                var attachments = scope.ServiceProvider.GetService<IAttachmentStore>();
                if (attachments is not null)
                {
                    var attRemoved = await attachments.DeleteOlderThanAsync(threshold, stoppingToken).ConfigureAwait(false);
                    if (attRemoved > 0)
                    {
                        logger.LogInformation("Data pruner removed {Count} attachments older than {Threshold}.", attRemoved, threshold);
                    }
                }

                var alertEvents = scope.ServiceProvider.GetService<IAlertEventStore>();
                if (alertEvents is not null)
                {
                    var alertRemoved = await alertEvents.DeleteOlderThanAsync(threshold, stoppingToken).ConfigureAwait(false);
                    if (alertRemoved > 0)
                    {
                        logger.LogInformation("Data pruner removed {Count} alert events older than {Threshold}.", alertRemoved, threshold);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Data pruner encountered an error.");
            }
        }
    }
}
