using Dialysis.SmartConnect.Attachments;

namespace Dialysis.SmartConnect.Persistence.ObjectStorage.Replication;

/// <summary>
/// Default <see cref="IAttachmentBlobReplicationStrategy"/> for single-region deployments — both
/// replicate methods are no-ops. Active by default so the attachment-store pipeline can invoke
/// the strategy unconditionally without a null check.
/// </summary>
public sealed class NoOpReplicationStrategy : IAttachmentBlobReplicationStrategy
{
    public ReplicationMode Mode => ReplicationMode.None;

    public Task ReplicateAsync(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ReplicateDeleteAsync(Guid attachmentId, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
