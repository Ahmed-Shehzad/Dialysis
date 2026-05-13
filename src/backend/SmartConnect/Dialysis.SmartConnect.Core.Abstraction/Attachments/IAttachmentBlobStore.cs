namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Low-level byte-storage seam. Default implementation persists bytes in the same EF row as the metadata
/// (<c>EfBytesAttachmentBlobStore</c>); future filesystem / object-storage implementations swap in here.
/// </summary>
public interface IAttachmentBlobStore
{
    Task WriteAsync(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    Task<ReadOnlyMemory<byte>?> ReadAsync(Guid attachmentId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid attachmentId, CancellationToken cancellationToken);
}
