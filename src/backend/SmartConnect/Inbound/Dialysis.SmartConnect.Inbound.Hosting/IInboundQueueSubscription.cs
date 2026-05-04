namespace Dialysis.SmartConnect.Inbound.Hosting;

/// <summary>
/// Produces <see cref="InboundQueueItem"/> instances for the <see cref="SmartConnectInboundQueueConsumer"/>.
/// </summary>
public interface IInboundQueueSubscription
{
    /// <summary>Blocks until an item is available or the channel completes.</summary>
    ValueTask<InboundQueueItem?> ReadAsync(CancellationToken cancellationToken);
}
