namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Options for <see cref="FileSystemAttachmentBlobStore"/>. <see cref="RootPath"/> must be an absolute
/// directory the host process can write to; it is created on demand. For multi-replica deployments,
/// point it at a shared NFS / SMB mount — the impl is stateless, but two replicas writing the same
/// id will race (an outer caller is responsible for id uniqueness via <c>Guid.CreateVersion7</c>).
/// </summary>
public sealed record FileSystemAttachmentBlobOptions
{
    /// <summary>
    /// Absolute path to the directory under which blobs are stored. Sharded as
    /// <c>&lt;RootPath&gt;/&lt;first-2-hex-chars-of-id&gt;/&lt;id&gt;.bin</c> so no single directory
    /// holds more than ~256 sibling subdirectories (ext4 / NTFS stay performant past millions of files).
    /// </summary>
    public required string RootPath { get; init; }
}
