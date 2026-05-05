namespace Dialysis.SmartConnect.Tests.TestPlugins;

public sealed class FailingOutboundAdapter : IOutboundAdapter
{
    public const string KindValue = "failing-outbound-test";

    public string Kind => KindValue;

    public Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken) =>
        Task.FromResult(new OutboundSendResult(false, "simulated outbound failure"));
}
