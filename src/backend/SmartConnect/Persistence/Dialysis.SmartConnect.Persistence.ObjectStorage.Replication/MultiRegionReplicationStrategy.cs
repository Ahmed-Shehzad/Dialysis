using Dialysis.SmartConnect.Attachments;
using Microsoft.Extensions.Logging;

namespace Dialysis.SmartConnect.Persistence.ObjectStorage.Replication;

/// <summary>
/// Fan-out replication across N secondary stores. The primary has already committed before this
/// strategy is invoked. Quorum semantics:
/// <list type="bullet">
///   <item><see cref="ReplicationMode.BestEffort"/>: fire-and-forget; log failures but don't propagate.</item>
///   <item><see cref="ReplicationMode.Quorum"/>: wait for ⌊N/2⌋+1 successes; throw if quorum missed.</item>
///   <item><see cref="ReplicationMode.All"/>: wait for every secondary; throw on any failure.</item>
/// </list>
/// </summary>
public sealed class MultiRegionReplicationStrategy : IAttachmentBlobReplicationStrategy
{
    private readonly IReadOnlyList<IAttachmentBlobStore> _secondaries;
    private readonly ILogger<MultiRegionReplicationStrategy> _logger;

    public MultiRegionReplicationStrategy(
        ReplicationMode mode,
        IReadOnlyList<IAttachmentBlobStore> secondaries,
        ILogger<MultiRegionReplicationStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(secondaries);
        ArgumentNullException.ThrowIfNull(logger);
        Mode = mode;
        _secondaries = secondaries;
        _logger = logger;
    }

    public ReplicationMode Mode { get; }

    public Task ReplicateAsync(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) =>
        FanOutAsync(
            s => s.WriteAsync(attachmentId, data, cancellationToken),
            "write replication",
            cancellationToken);

    public Task ReplicateDeleteAsync(Guid attachmentId, CancellationToken cancellationToken) =>
        FanOutAsync(
            s => s.DeleteAsync(attachmentId, cancellationToken),
            "delete replication",
            cancellationToken);

    private async Task FanOutAsync(
        Func<IAttachmentBlobStore, Task> operation, string operationName, CancellationToken cancellationToken)
    {
        if (_secondaries.Count == 0 || Mode == ReplicationMode.None)
        {
            return;
        }

        var tasks = _secondaries.Select(s => Task.Run(async () =>
        {
            try
            {
                await operation(s).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Replication failure during {Operation}", operationName);
                return false;
            }
        }, cancellationToken)).ToList();

        if (Mode == ReplicationMode.BestEffort)
        {
            // Fan and forget the tasks — wrap in a continuation so unobserved exceptions don't
            // crash the process, but don't block the caller.
            _ = Task.WhenAll(tasks).ContinueWith(
                t => _logger.LogDebug("Best-effort {Operation} drained: {Total} tasks complete", operationName, tasks.Count),
                TaskScheduler.Default);
            return;
        }

        var successes = (await Task.WhenAll(tasks).ConfigureAwait(false)).Count(b => b);
        var quorum = (_secondaries.Count / 2) + 1;
        var required = Mode == ReplicationMode.All ? _secondaries.Count : quorum;
        if (successes < required)
        {
            throw new InvalidOperationException(
                $"{operationName} fell below the required replication threshold ({successes}/{required}).");
        }
    }
}
