namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Default <see cref="IAttachmentDownloadUrlFactory"/> for stores that don't support signed URLs
/// (in-row, FileSystem in production). The attachment download endpoint falls back to streaming
/// bytes through the API host when this implementation is active.
/// </summary>
public sealed class NullAttachmentDownloadUrlFactory : IAttachmentDownloadUrlFactory
{
    public bool SupportsSignedUrls => false;

    public Task<Uri?> CreateAsync(Guid attachmentId, TimeSpan ttl, CancellationToken cancellationToken) =>
        Task.FromResult<Uri?>(null);
}
