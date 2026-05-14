namespace Dialysis.BuildingBlocks.Transponder.Transport;

/// <summary>
/// Broker-specific send/receive. Host integrations replace <see cref="ITransponderBus"/> and use a transport for cross-process messaging.
/// </summary>
public interface ITransponderTransport : IAsyncDisposable
{
    /// <summary>
    /// Ensures the underlying connection (and channels) exist before publish or consume.
    /// </summary>
    ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a message to the configured exchange using <see cref="TransportMessage.RoutingKey"/>.
    /// </summary>
    Task PublishAsync(TransportMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Declares topology, subscribes to the application queue, and invokes <paramref name="onMessage"/> for each delivery until <paramref name="cancellationToken"/> is cancelled.
    /// Implementations should acknowledge or negative-acknowledge according to handler success.
    /// </summary>
    Task RunConsumerAsync(
        Func<TransportMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken);
}
