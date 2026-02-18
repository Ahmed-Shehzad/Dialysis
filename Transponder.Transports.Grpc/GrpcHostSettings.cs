using Transponder.Transports.Abstractions;
using Transponder.Transports.Grpc.Abstractions;

namespace Transponder.Transports.Grpc;

/// <summary>
/// Default gRPC transport host settings.
/// </summary>
public sealed class GrpcHostSettings : TransportHostSettings, IGrpcHostSettings
{
    public GrpcHostSettings(
        Uri address,
        IGrpcTopology? topology = null,
        bool useTls = true,
        string? authority = null,
        int? maxReceiveMessageSize = null,
        int? maxSendMessageSize = null,
        TimeSpan? keepAliveTime = null,
        IReadOnlyDictionary<string, object?>? settings = null,
        TransportResilienceOptions? resilienceOptions = null)
        : base(address, settings, resilienceOptions)
    {
        Topology = topology ?? new GrpcTopology();
        UseTls = useTls;
        Authority = authority;
        MaxReceiveMessageSize = maxReceiveMessageSize;
        MaxSendMessageSize = maxSendMessageSize;
        KeepAliveTime = keepAliveTime;
    }

    public IGrpcTopology Topology { get; }

    public bool UseTls { get; }

    public string? Authority { get; }

    public int? MaxReceiveMessageSize { get; }

    public int? MaxSendMessageSize { get; }

    public TimeSpan? KeepAliveTime { get; }
}
