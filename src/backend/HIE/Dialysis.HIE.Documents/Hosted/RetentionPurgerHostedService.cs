using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Documents.Hosted;

/// <summary>Options for the retention purger background service.</summary>
public sealed class RetentionPurgerOptions
{
    /// <summary>
    /// When <c>true</c> the hosted service runs <see cref="IRetentionPurgeJob.RunOnceAsync"/>
    /// on a 24-hour tick. Default <c>false</c> so deployments don't accidentally start a
    /// background mutator before the operator has set policies.
    /// </summary>
    public bool AutoPurge { get; set; }

    /// <summary>Tick interval; default 24 hours.</summary>
    public TimeSpan TickInterval { get; set; } = TimeSpan.FromHours(24);
}

/// <summary>
/// Daily walker that runs the HIE retention purger. Mirrors
/// <c>LtvUpgraderHostedService</c> — opt-in via configuration so deployments don't accidentally
/// start a background mutator before the operator has defined retention policies.
/// </summary>
public sealed class RetentionPurgerHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly RetentionPurgerOptions _options;
    private readonly ILogger<RetentionPurgerHostedService> _logger;

    public RetentionPurgerHostedService(
        IServiceProvider services,
        IOptions<RetentionPurgerOptions> options,
        ILogger<RetentionPurgerHostedService> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.AutoPurge)
        {
            _logger.LogInformation(
                "Retention purger disabled — set Documents:Retention:AutoPurge=true to enable.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var job = scope.ServiceProvider.GetService<IRetentionPurgeJob>();
                if (job is null)
                {
                    _logger.LogWarning("No IRetentionPurgeJob registered; purger has nothing to do.");
                }
                else
                {
                    var purged = await job.RunOnceAsync(stoppingToken).ConfigureAwait(false);
                    _logger.LogInformation("Retention purger tick — {Count} document(s) purged.", purged);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retention purger tick failed; retrying on next interval.");
            }

            try
            {
                await Task.Delay(_options.TickInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        }
    }
}
