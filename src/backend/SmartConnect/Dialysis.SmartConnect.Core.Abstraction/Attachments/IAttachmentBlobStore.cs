namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Low-level byte-storage seam. Default implementation persists bytes in the same EF row as the metadata
/// (<c>InRowAttachmentBlobStore</c>); future filesystem / object-storage implementations swap in here.
/// </summary>
public interface IAttachmentBlobStore
{
    /// <summary>
    /// <c>true</c> when bytes live in the same physical row as the attachment metadata (the in-row default).
    /// Lets <c>EfAttachmentStore</c> persist metadata + bytes atomically in one transaction. Out-of-row
    /// backends (filesystem, S3, Azure Blob) return <c>false</c>; they require blob-first ordering and
    /// orphan reaping on metadata-save failure.
    /// </summary>
    bool StoresBytesInRow { get; }

    Task WriteAsync(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    /// <summary>
    /// Synchronous counterpart to <see cref="WriteAsync"/>. Exists so <see cref="IAttachmentStore.Add"/>
    /// can complete blob storage without a sync-over-async bridge; the JS <c>addAttachment</c> binding
    /// is the only intended caller of the sync chain.
    /// </summary>
    void Write(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    Task<ReadOnlyMemory<byte>?> ReadAsync(Guid attachmentId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid attachmentId, CancellationToken cancellationToken);

    /// <summary>
    /// Streams metadata for every blob the backend currently holds. The orphan reaper consumes this
    /// to find blobs without a matching metadata row and delete them after a grace window.
    /// In-row backends can return what's in the metadata table (no orphans are possible since bytes
    /// share the row); out-of-row backends walk their own storage (directory listing, S3 ListObjects).
    /// </summary>
    IAsyncEnumerable<BlobMetadata> EnumerateAsync(CancellationToken cancellationToken);
}
