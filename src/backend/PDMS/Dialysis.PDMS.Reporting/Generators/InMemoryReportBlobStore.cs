using System.Collections.Concurrent;

namespace Dialysis.PDMS.Reporting.Generators;

/// <summary>
/// In-memory <see cref="IReportBlobStore"/> for development and tests. Stores bytes by
/// a synthesised storage-ref key. Production hosts register an S3 / Azure-Blob-backed
/// implementation in the composition root; this implementation keeps single-host
/// development snappy without forcing object storage to be present.
/// </summary>
public sealed class InMemoryReportBlobStore : IReportBlobStore
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new(StringComparer.Ordinal);

    public Task<string> SaveAsync(Guid reportId, string contentType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        var storageRef = $"inmem://reports/{reportId:N}";
        _store[storageRef] = body.ToArray();
        return Task.FromResult(storageRef);
    }

    public Task<byte[]?> ReadAsync(string storageRef, CancellationToken cancellationToken)
    {
        _store.TryGetValue(storageRef, out var bytes);
        return Task.FromResult<byte[]?>(bytes);
    }
}
