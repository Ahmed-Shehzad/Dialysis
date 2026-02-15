using Transponder.Transports.Grpc.Abstractions;

namespace Transponder.Transports.Grpc;

/// <summary>
/// Default gRPC topology conventions.
/// </summary>
public sealed class GrpcTopology : IGrpcTopology
{
    public string ServiceName => "transponder.transport.Transport";

    public string SendMethodName => "Send";

    public string PublishMethodName => "Publish";
}
