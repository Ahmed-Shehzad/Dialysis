namespace Dialysis.SmartConnect;

/// <summary>
/// Sends a transformed message to an external system for one outbound route.
/// </summary>
public interface IOutboundAdapter
{
    string Kind { get; }

    Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken);
}

public readonly record struct OutboundSendResult(bool Succeeded, string? ErrorDetail, byte[]? ResponsePayload = null);
