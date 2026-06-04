using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.AwsSqsSns;

/// <summary>Publishes JSON payloads to an SQS queue using message attributes for CLR routing.</summary>
public sealed class AwsSqsTransponderBus : ITransponderBus
{
    private readonly ITransponderTransport _transport;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<AwsSqsTransponderBus> _logger;
    /// <summary>Publishes JSON payloads to an SQS queue using message attributes for CLR routing.</summary>
    public AwsSqsTransponderBus(ITransponderTransport transport,
        IMessageSerializer serializer,
        ILogger<AwsSqsTransponderBus> logger)
    {
        _transport = transport;
        _serializer = serializer;
        _logger = logger;
    }
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class =>
        PublishAsync(message, default, cancellationToken);

    public async Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        await _transport.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        var routingKey = typeof(TMessage).FullName ?? typeof(TMessage).Name;
        var payload = _serializer.Serialize(message);
        var correlation = options.CorrelationId ?? Guid.NewGuid().ToString("N");
        var deduplicationId = string.IsNullOrEmpty(options.DeduplicationId) ? correlation : options.DeduplicationId;
        var envelope = new TransportMessage(
            routingKey,
            payload,
            CorrelationId: correlation,
            ContentType: "application/json",
            DeduplicationId: deduplicationId);

        await _transport.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Published {MessageType} to SQS with correlation {CorrelationId}", typeof(TMessage).Name, correlation);
    }

    public async Task PublishPreparedAsync(
        string routingKey,
        object message,
        TransponderPublishOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _transport.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        var payload = _serializer.Serialize(message.GetType(), message);
        var correlation = options.CorrelationId ?? Guid.NewGuid().ToString("N");
        var deduplicationId = string.IsNullOrEmpty(options.DeduplicationId) ? correlation : options.DeduplicationId;
        var envelope = new TransportMessage(
            routingKey,
            payload,
            CorrelationId: correlation,
            ContentType: "application/json",
            DeduplicationId: deduplicationId);

        await _transport.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Published {RoutingKey} to SQS with correlation {CorrelationId}", routingKey, correlation);
    }

    public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default)
        where TMessage : class =>
        TransponderLargeMessagePublisher.PublishAsync(this, _serializer, message, options, cancellationToken);
}
