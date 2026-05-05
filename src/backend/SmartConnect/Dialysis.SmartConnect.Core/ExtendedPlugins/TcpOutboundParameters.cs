namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>Framing applied by <see cref="TcpOutboundAdapter"/> when writing the payload.</summary>
public enum TcpOutboundFraming
{
    /// <summary>Write the payload bytes verbatim.</summary>
    None = 0,

    /// <summary>Prefix the payload with a 4-byte big-endian length.</summary>
    LengthPrefixed = 1,

    /// <summary>Wrap the payload with the MLLP start (<c>0x0B</c>) and end (<c>0x1C 0x0D</c>) markers.</summary>
    Mllp = 2,
}

/// <summary>JSON parameter shape for <see cref="TcpOutboundAdapter"/>.</summary>
public sealed class TcpOutboundParameters
{
    public string? Host { get; set; }

    public int Port { get; set; }

    public TcpOutboundFraming Framing { get; set; } = TcpOutboundFraming.Mllp;

    public int ConnectTimeoutMs { get; set; } = 5_000;

    public int SendTimeoutMs { get; set; } = 10_000;

    public bool KeepConnectionOpen { get; set; }
}
