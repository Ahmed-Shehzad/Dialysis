namespace Dialysis.SmartConnect.Inbound.TcpListener;

/// <summary>
/// Parsed, validated parameters for <see cref="TcpListenerSourceConnector"/>.
/// </summary>
internal sealed class TcpListenerParameters
{
    public required string ListenAddress { get; init; }
    public required int ListenPort { get; init; }
    public required FrameDecodingMode Framing { get; init; }
    public required int MaxMessageBytes { get; init; }
    public required int MaxConnections { get; init; }

    public static TcpListenerParameters Parse(IReadOnlyDictionary<string, string> raw)
    {
        var address = raw.GetValueOrDefault("ListenAddress") ?? "0.0.0.0";
        if (!int.TryParse(raw.GetValueOrDefault("ListenPort") ?? "", out var port) || port is < 1 or > 65535)
        {
            throw new ArgumentException("ListenPort must be between 1 and 65535.");
        }

        var framingStr = raw.GetValueOrDefault("Framing") ?? "None";
        if (!Enum.TryParse<FrameDecodingMode>(framingStr, true, out var framing))
        {
            throw new ArgumentException($"Framing '{framingStr}' is not valid. Use None, LineFeed, Mllp, or LengthPrefixed.");
        }

        var maxBytes = 4 * 1024 * 1024; // 4 MB default
        if (raw.TryGetValue("MaxMessageBytes", out var maxBytesStr) && int.TryParse(maxBytesStr, out var parsed))
        {
            maxBytes = parsed;
        }

        var maxConns = 100;
        if (raw.TryGetValue("MaxConnections", out var maxConnsStr) && int.TryParse(maxConnsStr, out var parsedConns))
        {
            maxConns = parsedConns;
        }

        return new TcpListenerParameters
        {
            ListenAddress = address,
            ListenPort = port,
            Framing = framing,
            MaxMessageBytes = maxBytes,
            MaxConnections = maxConns,
        };
    }
}

/// <summary>Frame decoding strategy for TCP socket reads.</summary>
public enum FrameDecodingMode
{
    /// <summary>Each read is one message (up to MaxMessageBytes).</summary>
    None,

    /// <summary>Messages terminated by LF (0x0A).</summary>
    LineFeed,

    /// <summary>MLLP framing: 0x0B ... 0x1C 0x0D.</summary>
    Mllp,

    /// <summary>4-byte big-endian length prefix (uint32) followed by payload bytes.</summary>
    LengthPrefixed,
}
