namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

/// <summary>Server-side SignalR limits for the ingress hub.</summary>
public sealed class TransponderSignalRIngressOptions
{
    /// <summary>Maximum SignalR message size in bytes (default 32 MiB).</summary>
    public int MaximumReceiveMessageSizeBytes { get; set; } = 32 * 1024 * 1024;
}
