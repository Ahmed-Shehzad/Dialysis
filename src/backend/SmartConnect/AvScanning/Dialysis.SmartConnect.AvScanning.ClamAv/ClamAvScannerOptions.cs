namespace Dialysis.SmartConnect.AvScanning.ClamAv;

/// <summary>
/// Options for <see cref="ClamAvAttachmentBlobScanner"/>. The clamd default INSTREAM port is 3310;
/// the chunk size influences memory pressure but does not change the verdict.
/// </summary>
public sealed class ClamAvScannerOptions
{
    /// <summary>clamd hostname (or container DNS name).</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>clamd INSTREAM TCP port. Default 3310.</summary>
    public int Port { get; set; } = 3310;

    /// <summary>
    /// Chunk size used while streaming bytes to clamd. Default 64 KiB — clamd's StreamMaxLength
    /// default is 25 MB, and small chunks reduce peak memory but increase syscall overhead.
    /// </summary>
    public int ChunkSizeBytes { get; set; } = 64 * 1024;

    /// <summary>Connect + read timeout. Defaults to 30 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
