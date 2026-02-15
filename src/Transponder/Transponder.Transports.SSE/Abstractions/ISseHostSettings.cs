using Transponder.Transports.Abstractions;

namespace Transponder.Transports.SSE.Abstractions;

/// <summary>
/// Provides SSE specific settings for creating a transport host.
/// </summary>
public interface ISseHostSettings : ITransportHostSettings
{
    /// <summary>
    /// Gets the SSE topology conventions.
    /// </summary>
    ISseTopology Topology { get; }

    /// <summary>
    /// Gets the per-client buffer capacity for outbound events.
    /// </summary>
    int ClientBufferCapacity { get; }

    /// <summary>
    /// Gets the optional keep-alive interval for SSE connections.
    /// </summary>
    TimeSpan? KeepAliveInterval { get; }
}
