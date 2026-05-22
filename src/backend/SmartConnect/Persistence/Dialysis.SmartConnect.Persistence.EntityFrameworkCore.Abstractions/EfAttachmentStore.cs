using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

/// <summary>
/// EF Core <see cref="IAttachmentStore"/>. Persists metadata via <see cref="AttachmentEntity"/> rows and
/// delegates byte storage to the registered <see cref="IAttachmentBlobStore"/>. The default blob store
/// writes bytes into the same row (<c>EfBytesAttachmentBlobStore</c>); a future filesystem impl leaves
/// <see cref="AttachmentEntity.Data"/> null and stores bytes externally.
/// </summary>
public sealed class EfAttachmentStore(SmartConnectDbContext db, IAttachmentBlobStore blobs) : IAttachmentStore
{
    public async Task<Attachment> AddAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        var (entity, prepared) = PrepareInsert(attachment);
        db.Attachments.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await blobs.WriteAsync(entity.Id, prepared.Data, cancellationToken).ConfigureAwait(false);
        return prepared;
    }

    public Attachment Add(Attachment attachment, CancellationToken cancellationToken)
    {
        var (entity, prepared) = PrepareInsert(attachment);
        db.Attachments.Add(entity);
        db.SaveChanges();

        blobs.Write(entity.Id, prepared.Data, cancellationToken);
        return prepared;
    }

    private static (AttachmentEntity Entity, Attachment Prepared) PrepareInsert(Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        var id = attachment.Id == Guid.Empty ? Guid.CreateVersion7() : attachment.Id;
        var size = attachment.SizeBytes > 0 ? attachment.SizeBytes : attachment.Data.Length;
        var createdUtc = attachment.CreatedUtc == default ? DateTimeOffset.UtcNow : attachment.CreatedUtc;
        var mime = string.IsNullOrWhiteSpace(attachment.MimeType) ? "application/octet-stream" : attachment.MimeType;

        var entity = new AttachmentEntity
        {
            Id = id,
            MessageId = attachment.MessageId,
            FlowId = attachment.FlowId,
            MimeType = mime,
            SizeBytes = size,
            CreatedUtc = createdUtc,
        };
        var prepared = new Attachment
        {
            Id = id,
            MessageId = attachment.MessageId,
            FlowId = attachment.FlowId,
            MimeType = mime,
            Data = attachment.Data,
            SizeBytes = size,
            CreatedUtc = createdUtc,
        };
        return (entity, prepared);
    }

    public async Task<Attachment?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Attachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);
        if (entity is null) return null;

        var bytes = await blobs.ReadAsync(id, cancellationToken).ConfigureAwait(false);
        return new Attachment
        {
            Id = entity.Id,
            MessageId = entity.MessageId,
            FlowId = entity.FlowId,
            MimeType = entity.MimeType,
            Data = bytes ?? ReadOnlyMemory<byte>.Empty,
            SizeBytes = entity.SizeBytes,
            CreatedUtc = entity.CreatedUtc,
        };
    }

    public async Task<IReadOnlyList<Attachment>> GetForMessageAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var entities = await db.Attachments.AsNoTracking()
            .Where(a => a.MessageId == messageId)
            .OrderBy(a => a.CreatedUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (entities.Count == 0) return [];

        var result = new List<Attachment>(entities.Count);
        foreach (var entity in entities)
        {
            var bytes = await blobs.ReadAsync(entity.Id, cancellationToken).ConfigureAwait(false);
            result.Add(new Attachment
            {
                Id = entity.Id,
                MessageId = entity.MessageId,
                FlowId = entity.FlowId,
                MimeType = entity.MimeType,
                Data = bytes ?? ReadOnlyMemory<byte>.Empty,
                SizeBytes = entity.SizeBytes,
                CreatedUtc = entity.CreatedUtc,
            });
        }
        return result;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Attachments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);
        if (entity is null) return;
        db.Attachments.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await blobs.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteForMessageAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var entities = await db.Attachments.Where(a => a.MessageId == messageId).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (entities.Count == 0) return;
        db.Attachments.RemoveRange(entities);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var e in entities)
        {
            await blobs.DeleteAsync(e.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken)
    {
        var entities = await db.Attachments.Where(a => a.CreatedUtc < cutoffUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (entities.Count == 0) return 0;
        db.Attachments.RemoveRange(entities);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var e in entities)
        {
            await blobs.DeleteAsync(e.Id, cancellationToken).ConfigureAwait(false);
        }
        return entities.Count;
    }
}
