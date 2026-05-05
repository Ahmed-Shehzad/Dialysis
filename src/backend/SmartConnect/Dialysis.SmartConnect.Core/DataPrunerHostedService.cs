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
