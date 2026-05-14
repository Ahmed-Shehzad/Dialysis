namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

/// <summary>Client connection to a Transponder gRPC ingress host (see <see cref="TransponderGrpcServerExtensions"/>).</summary>
public sealed class TransponderGrpcClientOptions
{
    /// <summary>Base address of the ingress relay (e.g. https://localhost:7123).</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Optional name included in <see cref="SubscribeRequest.ClientName"/>.</summary>
    public string? ClientName { get; set; }

    /// <summary>Maximum inbound gRPC message size in bytes (default 32 MiB; align with relay <see cref="TransponderGrpcIngressOptions"/>).</summary>
    public int MaxReceiveMessageSizeBytes { get; set; } = 32 * 1024 * 1024;

    /// <summary>Maximum outbound gRPC message size in bytes (default 32 MiB).</summary>
    public int MaxSendMessageSizeBytes { get; set; } = 32 * 1024 * 1024;

    /// <summary>
    /// When true, accepts any server TLS certificate. **Development only** — do not use in production.
    /// Production should terminate TLS with a real certificate and validate it.
    /// </summary>
    public bool ForDevelopmentOnlyDisableCertificateValidation { get; set; }
}
