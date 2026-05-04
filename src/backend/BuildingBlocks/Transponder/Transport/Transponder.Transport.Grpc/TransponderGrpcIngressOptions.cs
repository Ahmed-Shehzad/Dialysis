namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

/// <summary>Server-side gRPC limits and diagnostics for the ingress relay (configured via <c>AddTransponderGrpcIngressServer</c>).</summary>
public sealed class TransponderGrpcIngressOptions
{
    /// <summary>Maximum inbound gRPC message size in bytes (default 32 MiB).</summary>
    public int MaxReceiveMessageSizeBytes { get; set; } = 32 * 1024 * 1024;

    /// <summary>Maximum outbound gRPC message size in bytes (default 32 MiB).</summary>
    public int MaxSendMessageSizeBytes { get; set; } = 32 * 1024 * 1024;

    /// <summary>When true, richer error detail is returned to clients (avoid in production).</summary>
    public bool EnableDetailedErrors { get; set; }
}
