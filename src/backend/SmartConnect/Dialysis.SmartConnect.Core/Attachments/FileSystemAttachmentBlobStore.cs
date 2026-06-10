using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Out-of-row <see cref="IAttachmentBlobStore"/> that writes each blob to a file on disk under
/// <see cref="FileSystemAttachmentBlobOptions.RootPath"/>. Suitable for single-clinic deployments
/// (or multi-replica deployments backed by a shared NFS/SMB mount). Pairs with the orphan reaper
/// so a metadata-save failure after the blob lands doesn't leave stranded files forever.
/// </summary>
public sealed class FileSystemAttachmentBlobStore : IAttachmentBlobStore
{
    private const string Extension = ".bin";
    private readonly string _rootPath;

    public FileSystemAttachmentBlobStore(IOptions<FileSystemAttachmentBlobOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var rootPath = options.Value.RootPath;
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("RootPath must be configured.", nameof(options));
        }
        _rootPath = rootPath;
    }

    public bool StoresBytesInRow => false;

    public async Task WriteAsync(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var path = ResolvePath(attachmentId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, data.ToArray(), cancellationToken).ConfigureAwait(false);
    }

    public void Write(Guid attachmentId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(attachmentId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, data.Span);
    }

    public async Task<ReadOnlyMemory<byte>?> ReadAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        var path = ResolvePath(attachmentId);
        if (!File.Exists(path))
            return null;
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return new ReadOnlyMemory<byte>(bytes);
    }

    public Task DeleteAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(attachmentId);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<BlobMetadata> EnumerateAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_rootPath))
            yield break;

        foreach (var file in Directory.EnumerateFiles(_rootPath, "*" + Extension, SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileNameWithoutExtension(file);
            if (!Guid.TryParse(name, out var id))
                continue;
            FileInfo info;
            try
            {
                info = new FileInfo(file);
                if (!info.Exists)
                    continue;
            }
            catch (FileNotFoundException)
            {
                continue;
            }
            yield return new BlobMetadata(id, info.CreationTimeUtc, info.Length);
            await Task.Yield();
        }
    }

    private string ResolvePath(Guid id)
    {
        var hex = id.ToString("N");
        var shard = hex[..2];
        return Path.Combine(_rootPath, shard, id + Extension);
    }
}
