namespace Dialysis.SmartConnect.Tests.TestPlugins;

public sealed class CapturingOutboundAdapter : IOutboundAdapter
{
    public const string KindValue = "capturing-test";

    public string Kind => KindValue;

    public List<(int Ordinal, ReadOnlyMemory<byte> Payload)> Sent { get; } = [];

    public Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken)
    {
        Sent.Add((outboundRouteOrdinal, message.Payload));
        return Task.FromResult(new OutboundSendResult(true, null));
    }
}
