using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Periodic background sweep that deletes blobs whose metadata insert never committed. Only active
/// when the registered <see cref="IAttachmentBlobStore"/> is out-of-row; in-row backends store bytes
/// and metadata in the same row, so orphans are impossible and the reaper exits each sweep early.
/// </summary>
/// <remarks>
/// Blob-first insert ordering means the bytes land before the metadata row. If the metadata save
/// fails, the bytes are stranded. This service walks the blob store, filters out blobs younger than
/// <see cref="AttachmentOrphanReaperOptions.GracePeriod"/> (those may still be mid-insert), batches
/// id lookups against the metadata table, and deletes any blob with no matching row — capped by
/// <see cref="AttachmentOrphanReaperOptions.MaxDeletionsPerSweep"/> so a wiped DB doesn't cascade
/// into a mass-delete.
/// </remarks>
public sealed class AttachmentOrphanReaperHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<AttachmentOrphanReaperOptions> options,
    TimeProvider timeProvider,
    ILogger<AttachmentOrphanReaperHostedService> logger) : BackgroundService
{
    private const int BatchSize = 100;
    private readonly AttachmentOrphanReaperOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.SweepInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await SweepAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Attachment orphan reaper sweep failed");
            }
        }
    }

    /// <summary>
    /// Runs a single sweep. Exposed so ops tooling and tests can trigger it without waiting for
    /// the next <see cref="AttachmentOrphanReaperOptions.SweepInterval"/> tick. Skips when the
    /// blob store is in-row (orphans impossible) and respects the safety caps regardless.
    /// </summary>
    public async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var blobs = scope.ServiceProvider.GetRequiredService<IAttachmentBlobStore>();
        if (blobs.StoresBytesInRow)
        {
            return;
        }
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();

        var cutoff = timeProvider.GetUtcNow() - _options.GracePeriod;
        var candidates = new List<BlobMetadata>();
        var collectionLimit = _options.MaxDeletionsPerSweep * 2;

        await foreach (var blob in blobs.EnumerateAsync(cancellationToken).ConfigureAwait(false))
        {
            if (blob.CreatedUtc >= cutoff) continue;
            candidates.Add(blob);
            if (candidates.Count >= collectionLimit) break;
        }

        if (candidates.Count == 0) return;

        var deletionsTotal = 0;
        foreach (var batch in candidates.Chunk(BatchSize))
        {
            if (deletionsTotal >= _options.MaxDeletionsPerSweep) break;
            var ids = batch.Select(c => c.Id).ToList();
            var existing = await db.Attachments.AsNoTracking()
                .Where(a => ids.Contains(a.Id))
                .Select(a => a.Id)
                .ToHashSetAsync(cancellationToken).ConfigureAwait(false);
            foreach (var blob in batch)
            {
                if (existing.Contains(blob.Id)) continue;
                if (deletionsTotal >= _options.MaxDeletionsPerSweep) break;
                await blobs.DeleteAsync(blob.Id, cancellationToken).ConfigureAwait(false);
                deletionsTotal++;
            }
        }

        if (deletionsTotal > 0)
        {
            logger.LogInformation("Reaped {Count} orphan attachment blobs", deletionsTotal);
        }
    }
}
