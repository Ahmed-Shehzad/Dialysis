using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Grpc.Abstractions;

/// <summary>
/// Provides gRPC specific settings for creating a transport host.
/// </summary>
public interface IGrpcHostSettings : ITransportHostSettings
{
    /// <summary>
    /// Gets the gRPC topology conventions.
    /// </summary>
    IGrpcTopology Topology { get; }

    /// <summary>
    /// Gets whether TLS is required for the transport.
    /// </summary>
    bool UseTls { get; }

    /// <summary>
    /// Gets the authority header override, if configured.
    /// </summary>
    string? Authority { get; }

    /// <summary>
    /// Gets the maximum receive message size in bytes, if configured.
    /// </summary>
    int? MaxReceiveMessageSize { get; }

    /// <summary>
    /// Gets the maximum send message size in bytes, if configured.
    /// </summary>
    int? MaxSendMessageSize { get; }

    /// <summary>
    /// Gets the keep-alive interval, if configured.
    /// </summary>
    TimeSpan? KeepAliveTime { get; }
}
