namespace Transponder.Transports.Grpc.Abstractions;

/// <summary>
/// Defines gRPC topology conventions.
/// </summary>
public interface IGrpcTopology
{
    /// <summary>
    /// Gets the gRPC service name.
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Gets the gRPC method name used for sends.
    /// </summary>
    string SendMethodName { get; }

    /// <summary>
    /// Gets the gRPC method name used for publishes.
    /// </summary>
    string PublishMethodName { get; }
}
