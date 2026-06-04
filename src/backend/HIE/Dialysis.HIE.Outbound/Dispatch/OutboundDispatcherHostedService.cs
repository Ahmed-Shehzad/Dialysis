using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Outbound.Dispatch;

/// <summary>
/// Periodically calls <see cref="IOutboundDispatcher.TickAsync"/>. Each tick opens its own scope so the
/// dispatcher gets a fresh <c>HieDbContext</c> + ITransponderOutbox transaction.
/// </summary>
public sealed class OutboundDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OutboundOptions> _options;
    private readonly ILogger<OutboundDispatcherHostedService> _logger;
    /// <summary>
    /// Periodically calls <see cref="IOutboundDispatcher.TickAsync"/>. Each tick opens its own scope so the
    /// dispatcher gets a fresh <c>HieDbContext</c> + ITransponderOutbox transaction.
    /// </summary>
    public OutboundDispatcherHostedService(IServiceScopeFactory scopeFactory,
        IOptions<OutboundOptions> options,
        ILogger<OutboundDispatcherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = _options.Value.TickInterval;
        if (interval <= TimeSpan.Zero) interval = TimeSpan.FromSeconds(10);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboundDispatcher>();
                var processed = await dispatcher.TickAsync(stoppingToken).ConfigureAwait(false);
                if (processed > 0)
                    _logger.LogDebug("Outbound dispatcher processed {Count} bundle(s)", processed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbound dispatcher tick failed");
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
