using Dialysis.BuildingBlocks.Documents.Signing.Ltv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Documents.Signing.Hosted;

/// <summary>
/// Background service that walks <c>DocumentReferenceSignature</c> rows still at
/// PAdES-B-T (or -B-LT) and runs the LTV augmenter on their PDF bytes, promoting them
/// to PAdES-B-LTA before the TSA's cert itself expires. The walker is driven through an
/// <see cref="ILtvUpgradeJob"/> the HIE module implements — the building block doesn't
/// know about <c>HieDbContext</c>.
///
/// Disabled by default. Hosts opt in by setting <c>Documents:Signing:Ltv:AutoUpgrade=true</c>.
/// </summary>
public sealed class LtvUpgraderHostedService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromHours(24);

    private readonly IServiceProvider _serviceProvider;
    private readonly LtvOptions _options;
    private readonly ILogger<LtvUpgraderHostedService> _logger;

    public LtvUpgraderHostedService(
        IServiceProvider serviceProvider,
        IOptions<LtvOptions> options,
        ILogger<LtvUpgraderHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.AutoUpgrade)
        {
            _logger.LogInformation("LtvUpgraderHostedService disabled — set Documents:Signing:Ltv:AutoUpgrade=true to enable.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var job = scope.ServiceProvider.GetService<ILtvUpgradeJob>();
                if (job is null)
                {
                    _logger.LogWarning("No ILtvUpgradeJob is registered; LTV upgrader has nothing to do.");
                }
                else
                {
                    var upgraded = await job.RunOnceAsync(stoppingToken).ConfigureAwait(false);
                    _logger.LogInformation("LTV upgrade pass complete — promoted {Count} signature(s).", upgraded);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LTV upgrader pass failed; will retry on next tick.");
            }

            try
            {
                await Task.Delay(TickInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        }
    }
}

/// <summary>
/// Module-supplied job that promotes outstanding PAdES-B-T signatures to LTA. The HIE
/// module implements this by streaming <c>DocumentReferenceSignature</c> rows, re-fetching
/// the PDF bytes through the shared <c>IDocumentBlobStore</c>, running the augmenter, and
/// updating the row via <c>DocumentReferenceSignature.UpgradeLevel</c>.
/// </summary>
public interface ILtvUpgradeJob
{
    /// <summary>Runs one upgrade pass; returns the number of rows promoted.</summary>
    Task<int> RunOnceAsync(CancellationToken cancellationToken);
}
