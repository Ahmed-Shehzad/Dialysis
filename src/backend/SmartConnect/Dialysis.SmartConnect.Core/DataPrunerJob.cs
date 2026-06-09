using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect;

/// <summary>
/// Persistent Hangfire job that prunes retention-expired data: message-ledger entries, attachments, and
/// alert events older than <see cref="DataPrunerOptions.RetentionPeriod"/>. Replaces the periodic
/// DataPrunerHostedService timer; Hangfire owns the schedule, retries, and failure surfacing.
/// </summary>
public sealed class DataPrunerJob
{
    private readonly DataPrunerOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _time;
    private readonly ILogger<DataPrunerJob> _logger;

    public DataPrunerJob(IServiceScopeFactory scopeFactory,
        IOptions<DataPrunerOptions> options,
        TimeProvider time,
        ILogger<DataPrunerJob> logger)
    {
        _scopeFactory = scopeFactory;
        _time = time;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>Runs one prune pass across the ledger, attachments, and alert-event stores.</summary>
    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var threshold = _time.GetUtcNow() - _options.RetentionPeriod;
        await using var scope = _scopeFactory.CreateAsyncScope();

        var ledger = scope.ServiceProvider.GetRequiredService<IMessageLedger>();
        var pruned = await ledger.PruneAsync(threshold, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (pruned > 0)
        {
            _logger.LogInformation("Data pruner removed {Count} ledger entries older than {Threshold}.", pruned, threshold);
        }

        var attachments = scope.ServiceProvider.GetService<IAttachmentStore>();
        if (attachments is not null)
        {
            var attRemoved = await attachments.DeleteOlderThanAsync(threshold, cancellationToken).ConfigureAwait(false);
            if (attRemoved > 0)
            {
                _logger.LogInformation("Data pruner removed {Count} attachments older than {Threshold}.", attRemoved, threshold);
            }
        }

        var alertEvents = scope.ServiceProvider.GetService<IAlertEventStore>();
        if (alertEvents is not null)
        {
            var alertRemoved = await alertEvents.DeleteOlderThanAsync(threshold, cancellationToken).ConfigureAwait(false);
            if (alertRemoved > 0)
            {
                _logger.LogInformation("Data pruner removed {Count} alert events older than {Threshold}.", alertRemoved, threshold);
            }
        }
    }
}
