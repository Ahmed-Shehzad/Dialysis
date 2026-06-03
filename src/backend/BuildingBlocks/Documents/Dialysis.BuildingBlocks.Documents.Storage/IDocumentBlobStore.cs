namespace Dialysis.BuildingBlocks.Documents.Storage;

/// <summary>
/// Persists clinical-document bytes outside the relational DB. Callers (PDMS reports, HIE
/// DocumentReference, EHR billing) hold only the storage-ref handle + a content hash on the
/// aggregate, and resolve back to the bytes through this port. Production hosts back the
/// port with S3 / Azure Blob; dev defaults to <see cref="InMemoryDocumentBlobStore"/> or
/// <see cref="FileSystemDocumentBlobStore"/>.
/// </summary>
public interface IDocumentBlobStore
{
    /// <summary>Persists <paramref name="body"/> and returns an opaque storage-ref the caller stores on the aggregate.</summary>
    Task<string> SaveAsync(Guid documentId, string contentType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken);

    /// <summary>Returns the bytes for <paramref name="storageRef"/>, or <c>null</c> if the ref is unknown.</summary>
    Task<byte[]?> ReadAsync(string storageRef, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the bytes for <paramref name="storageRef"/>. Returns true if a blob was removed.
    /// Callers must perform their own aggregate-level soft-delete bookkeeping; this is the raw byte purge.
    /// </summary>
    Task<bool> DeleteAsync(string storageRef, CancellationToken cancellationToken);
}
