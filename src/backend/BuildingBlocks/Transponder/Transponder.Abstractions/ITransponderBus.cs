namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Application-facing messaging API. Implementations provide transport-specific delivery; the default host uses in-process dispatch for development and tests.
/// </summary>
public interface ITransponderBus
{
    /// <summary>
    /// Dispatches <paramref name="message"/> to all registered <see cref="IConsumer{TMessage}"/> instances for <typeparamref name="TMessage"/>.
    /// </summary>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Publishes with explicit correlation (or other options). Transports may stamp a new correlation id when <see cref="TransponderPublishOptions.CorrelationId"/> is null.
    /// </summary>
    Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Publishes a message that is already deserialized. <paramref name="routingKey"/> must match the route registered for the message contract (same value as <see cref="Type.FullName"/> used by transports).
    /// </summary>
    Task PublishPreparedAsync(
        string routingKey,
        object message,
        TransponderPublishOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes <paramref name="message"/> once, splits into <see cref="TransponderMessageChunk"/> segments with a shared SHA-256 digest, and publishes each chunk.
    /// Small payloads are sent as a single <typeparamref name="TMessage"/> publish. Reassembly runs on hosts that registered <c>AddTransponder</c> (and broker transports must subscribe to <see cref="TransponderMessageChunk"/>; Rabbit/NATS extensions add this automatically).
    /// </summary>
    Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default)
        where TMessage : class;
}
