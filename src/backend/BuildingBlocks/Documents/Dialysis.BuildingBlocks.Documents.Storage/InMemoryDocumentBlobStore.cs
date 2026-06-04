using System.Collections.Concurrent;

namespace Dialysis.BuildingBlocks.Documents.Storage;

/// <summary>
/// In-process <see cref="IDocumentBlobStore"/> for development and tests. Stores bytes
/// by a synthesised storage-ref key under <c>inmem://documents/{id:N}</c>. Production hosts
/// register an S3 / Azure-Blob-backed implementation instead.
/// </summary>
public sealed class InMemoryDocumentBlobStore : IDocumentBlobStore
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new(StringComparer.Ordinal);

    public Task<string> SaveAsync(Guid documentId, string contentType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        var storageRef = $"inmem://documents/{documentId:N}";
        _store[storageRef] = body.ToArray();
        return Task.FromResult(storageRef);
    }

    public Task<byte[]?> ReadAsync(string storageRef, CancellationToken cancellationToken)
    {
        _store.TryGetValue(storageRef, out var bytes);
        return Task.FromResult<byte[]?>(bytes);
    }

    public Task<bool> DeleteAsync(string storageRef, CancellationToken cancellationToken) => Task.FromResult(_store.TryRemove(storageRef, out _));
}
