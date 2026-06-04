using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

/// <summary>
/// EF Core <see cref="IAttachmentStore"/>. Persists metadata via <see cref="AttachmentEntity"/> rows and
/// delegates byte storage to the registered <see cref="IAttachmentBlobStore"/>. The default blob store
/// writes bytes into the same row (<c>InRowAttachmentBlobStore</c>); a future filesystem impl leaves
/// <see cref="AttachmentEntity.Data"/> null and stores bytes externally.
/// </summary>
/// <remarks>
/// Insert strategy depends on <see cref="IAttachmentBlobStore.StoresBytesInRow"/>. For in-row backends
/// the bytes are written onto the entity before <c>SaveChanges</c>, making metadata + bytes atomic in
/// a single round-trip. For out-of-row backends the bytes go to the blob store first (orphan-on-failure
/// is cheaper to reap than orphan metadata that returns 404s for valid-looking ids), then the metadata
/// row is inserted; the orphan reaper sweeps any stranded blobs.
/// </remarks>
public sealed class EfAttachmentStore : IAttachmentStore
{
    private readonly SmartConnectDbContext _db;
    private readonly IAttachmentBlobStore _blobs;
    /// <summary>
    /// EF Core <see cref="IAttachmentStore"/>. Persists metadata via <see cref="AttachmentEntity"/> rows and
    /// delegates byte storage to the registered <see cref="IAttachmentBlobStore"/>. The default blob store
    /// writes bytes into the same row (<c>InRowAttachmentBlobStore</c>); a future filesystem impl leaves
    /// <see cref="AttachmentEntity.Data"/> null and stores bytes externally.
    /// </summary>
    /// <remarks>
    /// Insert strategy depends on <see cref="IAttachmentBlobStore.StoresBytesInRow"/>. For in-row backends
    /// the bytes are written onto the entity before <c>SaveChanges</c>, making metadata + bytes atomic in
    /// a single round-trip. For out-of-row backends the bytes go to the blob store first (orphan-on-failure
    /// is cheaper to reap than orphan metadata that returns 404s for valid-looking ids), then the metadata
    /// row is inserted; the orphan reaper sweeps any stranded blobs.
    /// </remarks>
    public EfAttachmentStore(SmartConnectDbContext db, IAttachmentBlobStore blobs)
    {
        _db = db;
        _blobs = blobs;
    }
    public async Task<Attachment> AddAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        var (entity, prepared) = PrepareInsert(attachment);

        if (_blobs.StoresBytesInRow)
        {
            entity.Data = prepared.Data.ToArray();
            _db.Attachments.Add(entity);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return prepared;
        }

        await _blobs.WriteAsync(entity.Id, prepared.Data, cancellationToken).ConfigureAwait(false);
        _db.Attachments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return prepared;
    }

    public Attachment Add(Attachment attachment, CancellationToken cancellationToken)
    {
        var (entity, prepared) = PrepareInsert(attachment);

        if (_blobs.StoresBytesInRow)
        {
            entity.Data = prepared.Data.ToArray();
            _db.Attachments.Add(entity);
            _db.SaveChanges();
            return prepared;
        }

        _blobs.Write(entity.Id, prepared.Data, cancellationToken);
        _db.Attachments.Add(entity);
        _db.SaveChanges();
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
        var entity = await _db.Attachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);
        if (entity is null) return null;

        var bytes = _blobs.StoresBytesInRow
            ? (entity.Data is null ? ReadOnlyMemory<byte>.Empty : new ReadOnlyMemory<byte>(entity.Data))
            : (await _blobs.ReadAsync(id, cancellationToken).ConfigureAwait(false)) ?? ReadOnlyMemory<byte>.Empty;
        return ProjectAttachment(entity, bytes);
    }

    public async Task<IReadOnlyList<Attachment>> GetForMessageAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var entities = await _db.Attachments.AsNoTracking()
            .Where(a => a.MessageId == messageId)
            .OrderBy(a => a.CreatedUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (entities.Count == 0) return [];

        var result = new List<Attachment>(entities.Count);
        if (_blobs.StoresBytesInRow)
        {
            foreach (var entity in entities)
            {
                var bytes = entity.Data is null ? ReadOnlyMemory<byte>.Empty : new ReadOnlyMemory<byte>(entity.Data);
                result.Add(ProjectAttachment(entity, bytes));
            }
            return result;
        }

        foreach (var entity in entities)
        {
            var bytes = (await _blobs.ReadAsync(entity.Id, cancellationToken).ConfigureAwait(false)) ?? ReadOnlyMemory<byte>.Empty;
            result.Add(ProjectAttachment(entity, bytes));
        }
        return result;
    }

    private static Attachment ProjectAttachment(AttachmentEntity entity, ReadOnlyMemory<byte> data) => new()
    {
        Id = entity.Id,
        MessageId = entity.MessageId,
        FlowId = entity.FlowId,
        MimeType = entity.MimeType,
        Data = data,
        SizeBytes = entity.SizeBytes,
        CreatedUtc = entity.CreatedUtc,
    };

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);
        if (entity is null) return;
        _db.Attachments.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _blobs.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteForMessageAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var entities = await _db.Attachments.Where(a => a.MessageId == messageId).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (entities.Count == 0) return;
        _db.Attachments.RemoveRange(entities);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var e in entities)
        {
            await _blobs.DeleteAsync(e.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken)
    {
        var entities = await _db.Attachments.Where(a => a.CreatedUtc < cutoffUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (entities.Count == 0) return 0;
        _db.Attachments.RemoveRange(entities);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var e in entities)
        {
            await _blobs.DeleteAsync(e.Id, cancellationToken).ConfigureAwait(false);
        }
        return entities.Count;
    }
}
