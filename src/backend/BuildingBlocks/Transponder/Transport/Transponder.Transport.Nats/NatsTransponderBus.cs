using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Nats;

/// <summary>
/// Publishes JSON payloads to NATS using <see cref="TransponderTransportHeaderNames.RoutingKey"/> for CLR type identity.
/// </summary>
public sealed class NatsTransponderBus(
    ITransponderTransport transport,
    IMessageSerializer serializer,
    ILogger<NatsTransponderBus> logger) : ITransponderBus
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class =>
        PublishAsync(message, default, cancellationToken);

    public async Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        await transport.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        var routingKey = typeof(TMessage).FullName ?? typeof(TMessage).Name;
        var payload = serializer.Serialize(message);
        var correlation = options.CorrelationId ?? Guid.NewGuid().ToString("N");
        var deduplicationId = string.IsNullOrEmpty(options.DeduplicationId) ? correlation : options.DeduplicationId;
        var envelope = new TransportMessage(
            routingKey,
            payload,
            CorrelationId: correlation,
            ContentType: "application/json",
            DeduplicationId: deduplicationId);

        await transport.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Published {MessageType} to NATS with correlation {CorrelationId}", typeof(TMessage).Name, correlation);
    }

    public async Task PublishPreparedAsync(
        string routingKey,
        object message,
        TransponderPublishOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        await transport.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        var payload = serializer.Serialize(message.GetType(), message);
        var correlation = options.CorrelationId ?? Guid.NewGuid().ToString("N");
        var deduplicationId = string.IsNullOrEmpty(options.DeduplicationId) ? correlation : options.DeduplicationId;
        var envelope = new TransportMessage(
            routingKey,
            payload,
            CorrelationId: correlation,
            ContentType: "application/json",
            DeduplicationId: deduplicationId);

        await transport.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Published {RoutingKey} to NATS with correlation {CorrelationId}", routingKey, correlation);
    }

    public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default)
        where TMessage : class =>
        TransponderLargeMessagePublisher.PublishAsync(this, serializer, message, options, cancellationToken);
}
