using Dialysis.SmartConnect.Attachments;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

/// <summary>
/// Default <see cref="IAttachmentBlobStore"/> that stores bytes in the <c>AttachmentEntity.Data</c> column.
/// Suitable until blobs grow past tens of MB; swap in a filesystem/object-storage impl by re-registering
/// <see cref="IAttachmentBlobStore"/> with a different implementation.
/// </summary>
public sealed class EfBytesAttachmentBlobStore(SmartConnectDbContext db) : IAttachmentBlobStore
{
    public async Task WriteAsync(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var entity = await db.Attachments.FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Attachment {attachmentId} does not exist; metadata row must be inserted first.");
        entity.Data = data.ToArray();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Write(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entity = db.Attachments.FirstOrDefault(a => a.Id == attachmentId)
            ?? throw new InvalidOperationException($"Attachment {attachmentId} does not exist; metadata row must be inserted first.");
        entity.Data = data.ToArray();
        db.SaveChanges();
    }

    public async Task<ReadOnlyMemory<byte>?> ReadAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        var bytes = await db.Attachments.AsNoTracking()
            .Where(a => a.Id == attachmentId)
            .Select(a => a.Data)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return bytes is null ? null : new ReadOnlyMemory<byte>(bytes);
    }

    public Task DeleteAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        // Row removal in EfAttachmentStore already wipes the bytes column.
        return Task.CompletedTask;
    }
}
