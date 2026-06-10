using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Persistent Hangfire job that deletes blobs whose metadata insert never committed. Only active when
/// the registered <see cref="IAttachmentBlobStore"/> is out-of-row; in-row backends store bytes and
/// metadata in the same row, so orphans are impossible and the reaper exits each sweep early.
/// </summary>
/// <remarks>
/// Blob-first insert ordering means the bytes land before the metadata row. If the metadata save fails,
/// the bytes are stranded. <see cref="SweepAsync"/> walks the blob store, filters out blobs younger than
/// <see cref="AttachmentOrphanReaperOptions.GracePeriod"/> (those may still be mid-insert), batches id
/// lookups against the metadata table, and deletes any blob with no matching row — capped by
/// <see cref="AttachmentOrphanReaperOptions.MaxDeletionsPerSweep"/> so a wiped DB doesn't cascade into a
/// mass-delete. Scheduled as a Hangfire recurring job; also callable directly by ops tooling and tests.
/// </remarks>
public sealed class AttachmentOrphanReaperJob
{
    private const int BatchSize = 100;
    private readonly AttachmentOrphanReaperOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AttachmentOrphanReaperJob> _logger;

    public AttachmentOrphanReaperJob(IServiceScopeFactory scopeFactory,
        IOptions<AttachmentOrphanReaperOptions> options,
        TimeProvider timeProvider,
        ILogger<AttachmentOrphanReaperJob> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Runs a single sweep. Skips when the blob store is in-row (orphans impossible) and respects the
    /// safety caps regardless.
    /// </summary>
    public async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var blobs = scope.ServiceProvider.GetRequiredService<IAttachmentBlobStore>();
        if (blobs.StoresBytesInRow)
        {
            return;
        }
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();

        var cutoff = _timeProvider.GetUtcNow() - _options.GracePeriod;
        var candidates = new List<BlobMetadata>();
        var collectionLimit = _options.MaxDeletionsPerSweep * 2;

        await foreach (var blob in blobs.EnumerateAsync(cancellationToken).ConfigureAwait(false))
        {
            if (blob.CreatedUtc >= cutoff)
                continue;
            candidates.Add(blob);
            if (candidates.Count >= collectionLimit)
                break;
        }

        if (candidates.Count == 0)
            return;

        var deletionsTotal = 0;
        foreach (var batch in candidates.Chunk(BatchSize))
        {
            if (deletionsTotal >= _options.MaxDeletionsPerSweep)
                break;
            var ids = batch.Select(c => c.Id).ToList();
            var existing = await db.Attachments.AsNoTracking()
                .Where(a => ids.Contains(a.Id))
                .Select(a => a.Id)
                .ToHashSetAsync(cancellationToken).ConfigureAwait(false);
            foreach (var blob in batch)
            {
                if (existing.Contains(blob.Id))
                    continue;
                if (deletionsTotal >= _options.MaxDeletionsPerSweep)
                    break;
                await blobs.DeleteAsync(blob.Id, cancellationToken).ConfigureAwait(false);
                deletionsTotal++;
            }
        }

        if (deletionsTotal > 0)
        {
            _logger.LogInformation("Reaped {Count} orphan attachment blobs", deletionsTotal);
        }
    }
}
