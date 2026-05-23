namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// AV / content inspection seam invoked before bytes hit storage. Implementations talk to ClamAV
/// (default), Microsoft Defender, or an enterprise AV gateway; a null implementation is registered
/// by default so hosts with no scanner remain functional.
/// </summary>
public interface IAttachmentBlobScanner
{
    /// <summary>
    /// Inspects the bytes and returns the verdict. Implementations must not throw on infected
    /// content — they return <see cref="AttachmentScanResult.Infected"/> with the threat name
    /// so the caller can quarantine and audit uniformly.
    /// </summary>
    Task<AttachmentScanResult> ScanAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken);
}

/// <summary>
/// Outcome of a scanner invocation. <see cref="ThreatName"/> is non-null only when
/// <see cref="Verdict"/> is <see cref="AttachmentScanVerdict.Infected"/>.
/// </summary>
public sealed record AttachmentScanResult(AttachmentScanVerdict Verdict, string? ThreatName)
{
    public static AttachmentScanResult Clean { get; } = new(AttachmentScanVerdict.Clean, null);

    public static AttachmentScanResult Infected(string threatName) =>
        new(AttachmentScanVerdict.Infected, threatName);

    public static AttachmentScanResult ScannerUnavailable { get; } =
        new(AttachmentScanVerdict.ScannerUnavailable, null);
}

/// <summary>Verdict from the scanner.</summary>
public enum AttachmentScanVerdict
{
    /// <summary>Scanner inspected the bytes and found nothing malicious.</summary>
    Clean = 0,

    /// <summary>Scanner found a threat — caller must reject the upload.</summary>
    Infected = 1,

    /// <summary>
    /// Scanner couldn't be reached. Caller decides whether to fail open (accept) or
    /// fail closed (reject) based on the deployment's risk tolerance.
    /// </summary>
    ScannerUnavailable = 2,
}
