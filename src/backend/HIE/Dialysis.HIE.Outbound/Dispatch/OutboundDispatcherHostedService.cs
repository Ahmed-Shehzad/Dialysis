using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Outbound.Dispatch;

/// <summary>
/// Periodically calls <see cref="IOutboundDispatcher.TickAsync"/>. Each tick opens its own scope so the
/// dispatcher gets a fresh <c>HieDbContext</c> + ITransponderOutbox transaction.
/// </summary>
public sealed class OutboundDispatcherHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboundOptions> options,
    ILogger<OutboundDispatcherHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.TickInterval;
        if (interval <= TimeSpan.Zero) interval = TimeSpan.FromSeconds(10);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboundDispatcher>();
                var processed = await dispatcher.TickAsync(stoppingToken).ConfigureAwait(false);
                if (processed > 0)
                    logger.LogDebug("Outbound dispatcher processed {Count} bundle(s)", processed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbound dispatcher tick failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
