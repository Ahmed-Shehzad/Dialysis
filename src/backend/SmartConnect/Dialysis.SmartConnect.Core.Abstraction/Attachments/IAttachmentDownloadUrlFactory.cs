namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Issues short-lived URLs the SPA (or any client) can use to fetch attachment bytes directly from
/// the underlying object store, bypassing the API host. Lets large attachments (DICOM, scanned
/// forms) flow over the CDN rather than re-streaming through ASP.NET memory. Per-store
/// implementations: S3 presigned, Azure SAS, FileSystem direct path (dev only).
/// </summary>
public interface IAttachmentDownloadUrlFactory
{
    /// <summary>
    /// <c>true</c> when the underlying store can mint redirectable URLs. Hosts default to streaming
    /// bytes through the API when this is <c>false</c> (in-row, filesystem in prod) so the
    /// attachment route stays uniform regardless of backend.
    /// </summary>
    bool SupportsSignedUrls { get; }

    /// <summary>
    /// Mints a signed download URL valid for <paramref name="ttl"/>. Returns <c>null</c> if the
    /// blob doesn't exist or the backend can't sign URLs (caller falls back to byte streaming).
    /// </summary>
    Task<Uri?> CreateAsync(Guid attachmentId, TimeSpan ttl, CancellationToken cancellationToken);
}
