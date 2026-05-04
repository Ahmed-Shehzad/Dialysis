namespace Dialysis.SmartConnect.BuiltInPlugins;

/// <summary>
/// Outbound adapter that performs no network I/O; used for tests and as a template sink.
/// </summary>
public sealed class PassThroughOutboundAdapter : IOutboundAdapter
{
    public const string KindValue = "pass-through";

    public string Kind => KindValue;

    public Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken) =>
        Task.FromResult(new OutboundSendResult(true, null));
}
