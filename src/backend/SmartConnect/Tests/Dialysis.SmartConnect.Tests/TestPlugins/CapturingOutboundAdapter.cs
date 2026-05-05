namespace Dialysis.SmartConnect.Tests.TestPlugins;

public sealed class CapturingOutboundAdapter : IOutboundAdapter
{
    public const string KindValue = "capturing-test";

    public string Kind => KindValue;

    public List<(int Ordinal, ReadOnlyMemory<byte> Payload)> Sent { get; } = [];

    /// <summary>When non-null, returned as <see cref="OutboundSendResult.ResponsePayload"/> on success.</summary>
    public byte[]? ResponseBytes { get; set; }

    public Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken)
    {
        Sent.Add((outboundRouteOrdinal, message.Payload));
        return Task.FromResult(new OutboundSendResult(true, null, ResponseBytes));
    }
}
