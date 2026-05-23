namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Cross-region durability strategy. Wraps a primary + zero-or-more secondary
/// <see cref="IAttachmentBlobStore"/>s; on write, the primary commits then the strategy decides how
/// hard to push the secondaries (best-effort vs. wait-for-quorum vs. all-or-nothing). On read, falls
/// back through the chain if the primary returns null.
/// </summary>
/// <remarks>
/// This is a coordination seam, not a replication transport. Real replication happens at the storage
/// platform level (S3 CRR, Azure GRS, B2 server-side copy). The strategy exists so the app layer
/// can opt into "write to both regions before acknowledging" semantics for HIPAA hot-cold setups
/// where the platform's async replication isn't enough.
/// </remarks>
public interface IAttachmentBlobReplicationStrategy
{
    /// <summary>Best-effort = fire-and-forget; Quorum = wait for majority; All = wait for everyone.</summary>
    ReplicationMode Mode { get; }

    /// <summary>
    /// Publishes the bytes to the secondaries. Primary has already committed by this point.
    /// Implementations decide whether to await (Quorum/All) or fan-out (BestEffort).
    /// </summary>
    Task ReplicateAsync(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the bytes from the secondaries. Symmetric to <see cref="ReplicateAsync"/>;
    /// honours the same <see cref="Mode"/> for back-pressure semantics.
    /// </summary>
    Task ReplicateDeleteAsync(Guid attachmentId, CancellationToken cancellationToken);
}

/// <summary>Replication consistency mode.</summary>
public enum ReplicationMode
{
    /// <summary>No replication — single region. Default for FileSystem/in-row.</summary>
    None = 0,

    /// <summary>Fan out writes, don't wait. Lowest latency, weakest guarantee.</summary>
    BestEffort = 1,

    /// <summary>Wait for majority. Tolerates one region down, durability still bounded.</summary>
    Quorum = 2,

    /// <summary>Wait for every secondary. Strongest guarantee, highest latency.</summary>
    All = 3,
}
