using Dialysis.SmartConnect.Attachments;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

/// <summary>
/// Default <see cref="IAttachmentBlobStore"/> that stores bytes in the <c>AttachmentEntity.Data</c> column.
/// Suitable for tests, demos, and small single-tenant deployments only — bytes share row pages with
/// metadata, so large attachments (multi-MB DICOM, PDFs) tank query latency. Production deployments
/// should re-register <see cref="IAttachmentBlobStore"/> with a filesystem / S3 / Azure Blob impl.
/// </summary>
/// <remarks>
/// Reports <see cref="StoresBytesInRow"/> = <c>true</c>, which lets <c>EfAttachmentStore</c> persist
/// metadata + bytes in a single transaction (one round-trip) rather than the two-phase insert that
/// out-of-row backends require. <see cref="WriteAsync"/> / <see cref="Write"/> remain functional for
/// callers that still drive the seam directly (e.g. legacy reattach flows).
/// </remarks>
public sealed class InRowAttachmentBlobStore(SmartConnectDbContext db) : IAttachmentBlobStore
{
    public bool StoresBytesInRow => true;

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
