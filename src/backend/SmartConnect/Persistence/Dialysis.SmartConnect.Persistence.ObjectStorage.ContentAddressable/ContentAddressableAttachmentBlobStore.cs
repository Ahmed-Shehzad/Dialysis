using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Persistence.ObjectStorage.ContentAddressable;

/// <summary>
/// Decorator over any <see cref="IAttachmentBlobStore"/> that adds content-addressable semantics:
/// identical bytes hash to one underlying blob with a reference count. Writing the same content
/// twice doesn't double-store; deleting one of N refs leaves the blob until the last ref is gone.
/// </summary>
/// <remarks>
/// The CAS key is the SHA-256 hex digest of the bytes. The underlying store sees calls keyed by a
/// synthetic <c>cas-{hash}</c>-derived Guid, not the attachment id — this lets multiple attachment
/// ids share a single object. Mapping rows live in <see cref="CasBlobRefEntity"/>; ref-count updates
/// happen in a transaction with the attachment metadata save.
/// </remarks>
public sealed class ContentAddressableAttachmentBlobStore : IAttachmentBlobStore
{
    private readonly IAttachmentBlobStore _inner;
    private readonly IServiceScopeFactory _scopeFactory;

    public ContentAddressableAttachmentBlobStore(IAttachmentBlobStore inner, IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        _inner = inner;
        _scopeFactory = scopeFactory;
    }

    public bool StoresBytesInRow => false;

    public async Task WriteAsync(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var hash = ComputeHash(data.Span);
        var blobId = HashToGuid(hash);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();

        var existing = await db.CasBlobRefs.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ContentHash == hash, cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            // Blob hasn't been written before — write underlying bytes, then record the first ref.
            await _inner.WriteAsync(blobId, data, cancellationToken).ConfigureAwait(false);
        }

        db.CasBlobRefs.Add(new CasBlobRefEntity
        {
            Id = Guid.CreateVersion7(),
            AttachmentId = attachmentId,
            ContentHash = hash,
            RefCount = 1,
        });
        // Bump ref count on the canonical row by rewriting it (in-row hash collisions impossible).
        if (existing is not null)
        {
            var canonical = await db.CasBlobRefs
                .FirstAsync(r => r.ContentHash == hash, cancellationToken).ConfigureAwait(false);
            canonical.RefCount += 1;
        }
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Not supported. CAS bookkeeping goes through EF Core, which is async-only — blocking on the
    /// async path here would deadlock under the ASP.NET sync context. Hosts wiring CAS must drive
    /// attachment storage through the async APIs; the Jint sync-write binding is only valid against
    /// stores whose entire pipeline (blob + ref bookkeeping) can complete synchronously.
    /// </summary>
    public void Write(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            "ContentAddressableAttachmentBlobStore does not support synchronous writes; call WriteAsync instead.");

    public async Task<ReadOnlyMemory<byte>?> ReadAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        var blobId = await ResolveBlobIdAsync(attachmentId, cancellationToken).ConfigureAwait(false);
        return blobId is null ? null : await _inner.ReadAsync(blobId.Value, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();

        var refRow = await db.CasBlobRefs
            .FirstOrDefaultAsync(r => r.AttachmentId == attachmentId, cancellationToken).ConfigureAwait(false);
        if (refRow is null)
            return;

        var hash = refRow.ContentHash;
        db.CasBlobRefs.Remove(refRow);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var remaining = await db.CasBlobRefs
            .CountAsync(r => r.ContentHash == hash, cancellationToken).ConfigureAwait(false);
        if (remaining == 0)
        {
            await _inner.DeleteAsync(HashToGuid(hash), cancellationToken).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<BlobMetadata> EnumerateAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Enumerate over attachment-level refs so the orphan reaper sees one entry per attachment
        // (not one per underlying blob). The reaper then matches against AttachmentEntity rows;
        // any ref whose attachment row vanished triggers DeleteAsync, which decrements the ref
        // count and removes the underlying blob only when no other attachment references it.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        var refs = await db.CasBlobRefs.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var r in refs)
        {
            yield return new BlobMetadata(r.AttachmentId, DateTimeOffset.UtcNow, 0);
        }
    }

    private async Task<Guid?> ResolveBlobIdAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
        var refRow = await db.CasBlobRefs.AsNoTracking()
            .FirstOrDefaultAsync(r => r.AttachmentId == attachmentId, cancellationToken).ConfigureAwait(false);
        return refRow is null ? null : HashToGuid(refRow.ContentHash);
    }

    private static string ComputeHash(ReadOnlySpan<byte> data)
    {
        Span<byte> digest = stackalloc byte[32];
        SHA256.HashData(data, digest);
        return Convert.ToHexStringLower(digest);
    }

    private static Guid HashToGuid(string hash)
    {
        // First 16 bytes of the SHA-256 digest become the Guid that keys the underlying store.
        // Collision probability at 2^64 attachments is still vanishingly small (birthday bound).
        Span<byte> bytes = stackalloc byte[16];
        Convert.FromHexString(hash.AsSpan(0, 32), bytes, out _, out _);
        return new Guid(bytes);
    }
}
