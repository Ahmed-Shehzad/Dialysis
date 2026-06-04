namespace Dialysis.SmartConnect.Tests.TestPlugins;

public sealed class CapturingOutboundAdapter : IOutboundAdapter
{
    public const string KindValue = "capturing-test";

    private readonly List<(int Ordinal, ReadOnlyMemory<byte> Payload)> _sent = [];

    public string Kind => KindValue;

    /// <summary>
    /// Snapshots every call to <see cref="SendAsync"/>. Reads under lock so the engine's parallel
    /// outbound dispatch (non-sequential pipelines) does not race on <c>List.Add</c>. The returned
    /// snapshot is ordered by call-completion time, NOT by route ordinal — tests that need a
    /// positional guarantee should filter by <c>Ordinal</c> rather than indexing.
    /// </summary>
    public IReadOnlyList<(int Ordinal, ReadOnlyMemory<byte> Payload)> Sent
    {
        get
        {
            lock (_sent)
            {
                return [.. _sent];
            }
        }
    }

    /// <summary>When non-null, returned as <see cref="OutboundSendResult.ResponsePayload"/> on success.</summary>
    public byte[]? ResponseBytes { get; set; }

    public Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken)
    {
        lock (_sent)
        {
            _sent.Add((outboundRouteOrdinal, message.Payload));
        }
        return Task.FromResult(new OutboundSendResult(true, null, ResponseBytes));
    }
}
