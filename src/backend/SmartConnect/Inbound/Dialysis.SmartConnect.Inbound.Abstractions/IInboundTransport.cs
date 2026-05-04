namespace Dialysis.SmartConnect.Inbound;

/// <summary>
/// Wraps <see cref="IFlowRuntime"/> for inbound paths and normalizes <see cref="FlowDispatchResult"/> to <see cref="InboundReceiveResult"/> with HTTP-friendly status hints.
/// </summary>
public interface IInboundTransport
{
    Task<InboundReceiveResult> DispatchAsync(IntegrationMessage message, CancellationToken cancellationToken);
}
