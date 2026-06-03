using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Documents.Storage;

/// <summary>
/// Options for <see cref="FileSystemDocumentBlobStore"/>.
/// </summary>
public sealed class FileSystemDocumentBlobStoreOptions
{
    /// <summary>Filesystem directory under which blobs are persisted. Created on first write.</summary>
    public string RootPath { get; set; } = "./.dev-blobs";
}

/// <summary>
/// Filesystem-backed <see cref="IDocumentBlobStore"/> for single-host development and
/// Aspire dev. Layout is one file per document; storage refs are <c>file://{documentId:N}</c>.
/// Not multi-host safe — production hosts use the S3 / Azure implementation.
/// </summary>
public sealed class FileSystemDocumentBlobStore : IDocumentBlobStore
{
    private readonly string _rootPath;

    public FileSystemDocumentBlobStore(IOptions<FileSystemDocumentBlobStoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _rootPath = options.Value.RootPath;
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Guid documentId, string contentType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        var name = $"{documentId:N}";
        var path = Path.Combine(_rootPath, name);
        await File.WriteAllBytesAsync(path, body.ToArray(), cancellationToken).ConfigureAwait(false);
        return $"file://{name}";
    }

    public async Task<byte[]?> ReadAsync(string storageRef, CancellationToken cancellationToken)
    {
        if (!TryResolvePath(storageRef, out var path) || !File.Exists(path))
        {
            return null;
        }
        return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> DeleteAsync(string storageRef, CancellationToken cancellationToken)
    {
        if (!TryResolvePath(storageRef, out var path) || !File.Exists(path))
        {
            return Task.FromResult(false);
        }
        File.Delete(path);
        return Task.FromResult(true);
    }

    private bool TryResolvePath(string storageRef, out string path)
    {
        const string prefix = "file://";
        if (string.IsNullOrEmpty(storageRef) || !storageRef.StartsWith(prefix, StringComparison.Ordinal))
        {
            path = string.Empty;
            return false;
        }
        var name = storageRef[prefix.Length..];
        // Guard against directory traversal — refs are minted as bare GUID hex by SaveAsync.
        if (name.Contains('/', StringComparison.Ordinal) || name.Contains('\\', StringComparison.Ordinal) || name.Contains("..", StringComparison.Ordinal))
        {
            path = string.Empty;
            return false;
        }
        path = Path.Combine(_rootPath, name);
        return true;
    }
}
