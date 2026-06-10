using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Dialysis.SmartConnect.Attachments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.AvScanning.ClamAv;

/// <summary>
/// Talks to clamd via the INSTREAM TCP protocol. Wire format:
/// <c>zINSTREAM\0</c> then a series of [uint32-big-endian length][bytes] chunks, terminated by a
/// zero-length chunk; clamd responds with <c>stream: OK</c> or <c>stream: &lt;ThreatName&gt; FOUND</c>.
/// </summary>
/// <remarks>
/// We deliberately avoid an external clamd client library — the protocol is two dozen lines of code
/// and a dependency on a third-party client would add another supply-chain risk to the AV pipeline.
/// </remarks>
public sealed class ClamAvAttachmentBlobScanner : IAttachmentBlobScanner
{
    private static readonly byte[] _instreamCommand = "zINSTREAM\0"u8.ToArray();
    private static readonly byte[] _endOfStream = [0, 0, 0, 0];
    private readonly ClamAvScannerOptions _options;
    private readonly ILogger<ClamAvAttachmentBlobScanner> _logger;

    public ClamAvAttachmentBlobScanner(
        IOptions<ClamAvScannerOptions> options,
        ILogger<ClamAvAttachmentBlobScanner> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AttachmentScanResult> ScanAsync(
        ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.Timeout);

            await client.ConnectAsync(_options.Host, _options.Port, cts.Token).ConfigureAwait(false);
            var stream = client.GetStream();

            await stream.WriteAsync(_instreamCommand, cts.Token).ConfigureAwait(false);
            await StreamChunksAsync(stream, data, cts.Token).ConfigureAwait(false);
            await stream.WriteAsync(_endOfStream, cts.Token).ConfigureAwait(false);
            await stream.FlushAsync(cts.Token).ConfigureAwait(false);

            var response = await ReadResponseAsync(stream, cts.Token).ConfigureAwait(false);
            return InterpretResponse(response);
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "ClamAV scanner unreachable at {Host}:{Port}", _options.Host, _options.Port);
            return AttachmentScanResult.ScannerUnavailable;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "ClamAV scan timed out after {Timeout}", _options.Timeout);
            return AttachmentScanResult.ScannerUnavailable;
        }
    }

    private async Task StreamChunksAsync(
        NetworkStream stream, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var lengthBuffer = new byte[4];
        var offset = 0;
        while (offset < data.Length)
        {
            var size = Math.Min(_options.ChunkSizeBytes, data.Length - offset);
            BinaryPrimitives.WriteUInt32BigEndian(lengthBuffer, (uint)size);
            await stream.WriteAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(data.Slice(offset, size), cancellationToken).ConfigureAwait(false);
            offset += size;
        }
    }

    private static async Task<string> ReadResponseAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        // INSTREAM responses are short (under 256 bytes); read in one shot rather than streaming.
        var buffer = new byte[512];
        var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        return Encoding.ASCII.GetString(buffer, 0, read).Trim('\0', '\n', ' ');
    }

    private static AttachmentScanResult InterpretResponse(string response)
    {
        if (response.EndsWith("OK", StringComparison.Ordinal))
        {
            return AttachmentScanResult.Clean;
        }
        if (response.EndsWith("FOUND", StringComparison.Ordinal))
        {
            // Response shape: "stream: <ThreatName> FOUND"
            var colon = response.IndexOf(':', StringComparison.Ordinal);
            var found = response.LastIndexOf(" FOUND", StringComparison.Ordinal);
            var threat = colon >= 0 && found > colon
                ? response.Substring(colon + 1, found - colon - 1).Trim()
                : "Unknown";
            return AttachmentScanResult.Infected(threat);
        }
        // Unknown response — treat as scanner-unavailable rather than silently passing.
        return AttachmentScanResult.ScannerUnavailable;
    }
}
