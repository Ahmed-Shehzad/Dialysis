namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Default <see cref="IAttachmentBlobScanner"/> that always reports clean. Registered when no real
/// scanner is wired so hosts without AV (dev / single-tenant clinics with file-system-only storage)
/// still pass the attachment pipeline. Replace with <c>UseClamAvAttachmentBlobScanner</c> for prod.
/// </summary>
public sealed class NullAttachmentBlobScanner : IAttachmentBlobScanner
{
    public Task<AttachmentScanResult> ScanAsync(
        ReadOnlyMemory<byte> data, CancellationToken cancellationToken) =>
        Task.FromResult(AttachmentScanResult.Clean);
}
