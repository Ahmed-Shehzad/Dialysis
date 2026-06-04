using StackExchange.Redis;

namespace Dialysis.BuildingBlocks.Documents.Storage.Valkey;

/// <summary>
/// Valkey/Redis-backed <see cref="IDocumentBlobStore"/>. Unlike the in-memory and filesystem
/// stores, this resolves the same bytes from every module process in the modular monolith —
/// PDMS writes a report blob and HIE's Documents board reads it back by the same storage-ref.
///
/// Keys live under a fixed, module-independent namespace — deliberately NOT the per-module
/// <c>IDistributedCache</c> InstanceName prefix — so cross-module sharing actually works against
/// the one shared Valkey instance. Bytes are persisted without expiry (clinical documents are
/// not cache entries that may be evicted); production hosts still swap in S3 / Azure Blob behind
/// this same interface.
/// </summary>
public sealed class ValkeyDocumentBlobStore : IDocumentBlobStore
{
    // Shared across every module — must not vary by module, or PDMS-written blobs become
    // invisible to HIE. This is the whole point of the Valkey store over the in-memory one.
    private const string KeyPrefix = "dialysis:doc-blobs:";

    private readonly IConnectionMultiplexer _multiplexer;

    /// <summary>Creates the store over the shared Valkey connection multiplexer.</summary>
    public ValkeyDocumentBlobStore(IConnectionMultiplexer multiplexer)
    {
        ArgumentNullException.ThrowIfNull(multiplexer);
        _multiplexer = multiplexer;
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(Guid documentId, string contentType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        var storageRef = $"valkey://documents/{documentId:N}";
        await _multiplexer.GetDatabase()
            .StringSetAsync(KeyFor(storageRef), body.ToArray())
            .ConfigureAwait(false);
        return storageRef;
    }

    /// <inheritdoc />
    public async Task<byte[]?> ReadAsync(string storageRef, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRef);
        var value = await _multiplexer.GetDatabase()
            .StringGetAsync(KeyFor(storageRef))
            .ConfigureAwait(false);
        return value.IsNull ? null : (byte[]?)value;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string storageRef, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRef);
        return await _multiplexer.GetDatabase()
            .KeyDeleteAsync(KeyFor(storageRef))
            .ConfigureAwait(false);
    }

    private static RedisKey KeyFor(string storageRef) => KeyPrefix + storageRef;
}
