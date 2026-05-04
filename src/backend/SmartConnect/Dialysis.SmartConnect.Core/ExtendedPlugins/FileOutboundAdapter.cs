using System.Text.Json;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>Writes payload to a file path from outbound parameters JSON.</summary>
public sealed class FileOutboundAdapter : IOutboundAdapter
{
    public string Kind => "file";

    public async Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken)
    {
        if (!message.Metadata.TryGetValue(HttpOutboundAdapter.ParametersMetadataKey, out var json) ||
            string.IsNullOrWhiteSpace(json))
        {
            return new OutboundSendResult(false, "File outbound requires parameters JSON with Path.");
        }

        var opts = JsonSerializer.Deserialize<FileOutboundParameters>(json);
        if (opts is null || string.IsNullOrWhiteSpace(opts.Path))
        {
            return new OutboundSendResult(false, "File outbound parameters must include Path.");
        }

        try
        {
            var bytes = message.Payload.ToArray();
            if (opts.Append)
            {
                await using var stream = new FileStream(
                    opts.Path,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);
                await stream.WriteAsync(bytes.AsMemory(0, bytes.Length), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await File.WriteAllBytesAsync(opts.Path, bytes, cancellationToken).ConfigureAwait(false);
            }

            return new OutboundSendResult(true, null);
        }
        catch (Exception ex)
        {
            return new OutboundSendResult(false, ex.Message);
        }
    }
}
