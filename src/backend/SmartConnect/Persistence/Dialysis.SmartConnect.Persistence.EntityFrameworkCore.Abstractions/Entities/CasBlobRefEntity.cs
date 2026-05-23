namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

/// <summary>
/// Content-addressable storage reference. Maps an <see cref="AttachmentEntity.Id"/> to the SHA-256
/// content hash of its bytes, with a ref-count so multiple attachments sharing the same content
/// resolve to one physical blob. The CAS decorator increments / decrements <see cref="RefCount"/>;
/// the underlying blob is removed when the count reaches zero.
/// </summary>
public sealed class CasBlobRefEntity
{
    /// <summary>Surrogate primary key. The natural key is <see cref="AttachmentId"/>.</summary>
    public Guid Id { get; set; }

    /// <summary>The attachment id this ref belongs to. Unique per row.</summary>
    public Guid AttachmentId { get; set; }

    /// <summary>SHA-256 of the content, hex-encoded lowercase (64 chars). Indexed for hash lookups.</summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// How many attachments currently reference <see cref="ContentHash"/>. Maintained transactionally
    /// by the CAS decorator. The blob is deleted from the underlying store when this hits zero.
    /// </summary>
    public int RefCount { get; set; }
}
