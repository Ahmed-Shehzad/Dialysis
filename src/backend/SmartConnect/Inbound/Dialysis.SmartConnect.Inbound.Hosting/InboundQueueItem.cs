using System.Collections.Immutable;
using Dialysis.SmartConnect;

namespace Dialysis.SmartConnect.Inbound.Hosting;

/// <summary>
/// One item read from an <see cref="IInboundQueueSubscription"/> for dispatch through SmartConnect.
/// </summary>
public sealed class InboundQueueItem
{
    public required Guid FlowId { get; init; }

    public required byte[] Payload { get; init; }

    public required PayloadFormat PayloadFormat { get; init; }

    public string? CorrelationId { get; init; }

    public ImmutableDictionary<string, string> Metadata { get; init; } =
        ImmutableDictionary<string, string>.Empty;
}
