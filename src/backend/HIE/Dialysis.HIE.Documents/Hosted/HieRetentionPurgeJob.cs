using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Documents.Ports;
using Microsoft.Extensions.Logging;

namespace Dialysis.HIE.Documents.Hosted;

/// <summary>
/// HIE implementation of <see cref="IRetentionPurgeJob"/>. One pass walks every operator-defined
/// <c>DocumentRetentionPolicy</c>, finds the Current documents of that kind whose
/// <c>CreatedAtUtc + RetentionDays &lt; now</c>, marks them entered-in-error, and deletes the
/// underlying blob bytes. The aggregate's <c>MarkBlobPurged("retention")</c> mutator atomically
/// soft-deletes the row and tombstones the storage ref so an auditor can prove the row was
/// purged deliberately rather than silently lost.
/// </summary>
public sealed class HieRetentionPurgeJob : IRetentionPurgeJob
{
    private const int BatchSize = 200;
    private readonly IDocumentRetentionPolicyRepository _policies;
    private readonly IDocumentReferenceRepository _documents;
    private readonly IDocumentBlobStore _blobs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly ILogger<HieRetentionPurgeJob> _logger;

    public HieRetentionPurgeJob(
        IDocumentRetentionPolicyRepository policies,
        IDocumentReferenceRepository documents,
        IDocumentBlobStore blobs,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        ILogger<HieRetentionPurgeJob> logger)
    {
        _policies = policies;
        _documents = documents;
        _blobs = blobs;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var policies = await _policies.ListAsync(cancellationToken).ConfigureAwait(false);
        if (policies.Count == 0)
        {
            _logger.LogInformation("Retention purge — no policies defined; nothing to do.");
            return 0;
        }

        var purgedTotal = 0;
        foreach (var policy in policies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cutoff = now.AddDays(-policy.RetentionDays);
            var expired = await _documents
                .ListExpiredAsync(policy.Kind, cutoff, BatchSize, cancellationToken)
                .ConfigureAwait(false);
            foreach (var doc in expired)
            {
                await _blobs.DeleteAsync(doc.StorageRef, cancellationToken).ConfigureAwait(false);
                doc.MarkBlobPurged("retention");
                purgedTotal++;
            }
            if (expired.Count > 0)
            {
                _logger.LogInformation(
                    "Retention purge — {Count} {Kind} document(s) purged at cutoff {Cutoff}",
                    expired.Count, policy.Kind, cutoff);
            }
        }

        if (purgedTotal > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return purgedTotal;
    }
}
