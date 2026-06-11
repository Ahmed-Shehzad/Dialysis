using Dialysis.BuildingBlocks.Transponder.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;

/// <summary>
/// RabbitMQ <see cref="ITransponderTransport"/> using separate publish and consumer channels.
/// </summary>
public sealed class RabbitMqTransponderTransport : ITransponderTransport
{
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private readonly SemaphoreSlim _dispatch = new(1, 1);
    private IConnection? _connection;
    private IChannel? _publishChannel;
    private IChannel? _consumeChannel;
    private const string TransportName = "rabbitmq";

    private readonly IOptions<TransponderRabbitMqOptions> _options;
    private readonly RabbitMqSubscriptionRegistry _registry;
    private readonly IEnumerable<IConsumeRouteMetadata> _consumeRoutes;
    private readonly ILogger<RabbitMqTransponderTransport> _logger;
    private readonly ITransponderStateObserver _stateObserver;
    /// <summary>
    /// RabbitMQ <see cref="ITransponderTransport"/> using separate publish and consumer channels.
    /// </summary>
    public RabbitMqTransponderTransport(IOptions<TransponderRabbitMqOptions> options,
        RabbitMqSubscriptionRegistry registry,
        IEnumerable<IConsumeRouteMetadata> consumeRoutes,
        ILogger<RabbitMqTransponderTransport> logger,
        ITransponderStateObserver? stateObserver = null)
    {
        _options = options;
        _registry = registry;
        _consumeRoutes = consumeRoutes;
        _logger = logger;
        _stateObserver = stateObserver ?? NullTransponderStateObserver.Instance;
    }

    public async ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection is { IsOpen: true } && _publishChannel is { IsOpen: true } && _consumeChannel is { IsOpen: true })
                return;

            await DisposeCoreAsync().ConfigureAwait(false);
            _stateObserver.OnTransportConnectionStateChanged(TransportName, TransponderTransportConnectionState.Connecting);

            var o = _options.Value;
            var factory = new ConnectionFactory { Uri = new Uri(o.ConnectionUri, UriKind.Absolute) };
            _connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

            var publishOpts = new CreateChannelOptions(
                publisherConfirmationsEnabled: o.PublisherConfirmsEnabled,
                publisherConfirmationTrackingEnabled: o.PublisherConfirmsEnabled,
                outstandingPublisherConfirmationsRateLimiter: null,
                consumerDispatchConcurrency: null);
            _publishChannel = await _connection.CreateChannelAsync(publishOpts, cancellationToken: cancellationToken).ConfigureAwait(false);
            _consumeChannel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            await _publishChannel
                .ExchangeDeclareAsync(
                    exchange: o.ExchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    passive: false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await _consumeChannel
                .ExchangeDeclareAsync(
                    exchange: o.ExchangeName,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    passive: false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Transponder RabbitMQ connected; exchange {Exchange}, publisher confirms {Confirms}",
                o.ExchangeName,
                o.PublisherConfirmsEnabled);
            _stateObserver.OnTransportConnectionStateChanged(TransportName, TransponderTransportConnectionState.Connected);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _stateObserver.OnTransportConnectionStateChanged(TransportName, TransponderTransportConnectionState.Faulted, ex);
            throw;
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public async Task PublishAsync(TransportMessage message, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var o = _options.Value;
        var ch = _publishChannel ?? throw new InvalidOperationException("Publish channel is not initialized.");

        await ch
            .BasicPublishAsync(
                exchange: o.ExchangeName,
                routingKey: message.RoutingKey,
                mandatory: false,
                basicProperties: CreateProps(message),
                body: message.Payload,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RunConsumerAsync(
        Func<TransportMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onMessage);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        var o = _options.Value;
        var consume = _consumeChannel ?? throw new InvalidOperationException("Consumer channel is not initialized.");

        var deadLetterConfigured = !string.IsNullOrWhiteSpace(o.DeadLetterFanoutExchangeName)
            && !string.IsNullOrWhiteSpace(o.DeadLetterQueueName);

        if (deadLetterConfigured)
        {
            var dlx = o.DeadLetterFanoutExchangeName!.Trim();
            var dlq = o.DeadLetterQueueName!.Trim();

            await consume
                .ExchangeDeclareAsync(
                    exchange: dlx,
                    type: ExchangeType.Fanout,
                    durable: true,
                    autoDelete: false,
                    passive: false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await consume
                .QueueDeclareAsync(
                    queue: dlq,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    passive: false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await consume
                .QueueBindAsync(
                    queue: dlq,
                    exchange: dlx,
                    routingKey: string.Empty,
                    arguments: null,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "Transponder RabbitMQ dead-letter fanout {Dlx} bound to queue {Dlq}",
                dlx,
                dlq);
        }
        else if (o.PoisonMessagePolicy == RabbitMqPoisonMessagePolicy.DeadLetter)
        {
            _logger.LogWarning(
                "PoisonMessagePolicy is DeadLetter but DeadLetterFanoutExchangeName/DeadLetterQueueName are not set; failures will be requeued.");
        }

        var queueArgs = BuildQueueArguments(o, deadLetterConfigured);
        await consume
            .QueueDeclareAsync(
                queue: o.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                passive: false,
                arguments: queueArgs,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Bind each routing key on the EXCHANGE THE PRODUCER PUBLISHES TO, not the local one.
        // Cross-module integration events live in `Dialysis.<Producer>.Contracts.*` namespaces;
        // the producer publishes on `dialysis.<producer>.events`. Binding the consumer queue
        // there is what makes events actually reach a remote module's consumers. Falls back to
        // the local exchange for transport-internal types like TransponderMessageChunk whose
        // namespace doesn't match a module.
        var bindings = new HashSet<(string Exchange, string RoutingKey)>();
        foreach (var kvp in _registry.RoutingKeyToType)
        {
            var exchange = ResolveProducerExchange(kvp.Value, o.ExchangeName);
            bindings.Add((exchange, kvp.Key));
        }
        foreach (var route in _consumeRoutes)
        {
            var rk = route.MessageType.FullName ?? route.MessageType.Name;
            var exchange = ResolveProducerExchange(route.MessageType, o.ExchangeName);
            bindings.Add((exchange, rk));
        }

        // Declare every foreign producer exchange before binding to it. Idempotent on the broker
        // side; safe even when the producing module hasn't started yet.
        foreach (var ex in bindings.Select(b => b.Exchange).Distinct(StringComparer.Ordinal))
        {
            if (string.Equals(ex, o.ExchangeName, StringComparison.Ordinal))
                continue; // already declared above
            await consume
                .ExchangeDeclareAsync(
                    exchange: ex,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    passive: false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var (exchange, routingKey) in bindings)
        {
            await consume
                .QueueBindAsync(
                    queue: o.QueueName,
                    exchange: exchange,
                    routingKey: routingKey,
                    arguments: null,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        if (bindings.Count > 0)
        {
            var foreignCount = bindings.Count(b => !string.Equals(b.Exchange, o.ExchangeName, StringComparison.Ordinal));
            _logger.LogInformation(
                "Transponder RabbitMQ bound queue {Queue} to {Total} routing keys ({Foreign} on foreign exchanges).",
                o.QueueName,
                bindings.Count,
                foreignCount);
        }

        var consumer = new AsyncEventingBasicConsumer(consume);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            await _dispatch.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var deduplicationId = ea.BasicProperties?.MessageId ?? ea.BasicProperties?.CorrelationId;
                var transportMessage = new TransportMessage(
                    ea.RoutingKey ?? string.Empty,
                    ea.Body,
                    ea.BasicProperties?.CorrelationId,
                    ea.BasicProperties?.ContentType ?? "application/json",
                    DeduplicationId: deduplicationId);

                await onMessage(transportMessage, cancellationToken).ConfigureAwait(false);
                await consume.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var requeue = ComputeRequeueOnFailure(o, deadLetterConfigured);
                _logger.LogError(ex, "Transponder consumer handler failed; nack requeue={Requeue}", requeue);
                await consume
                    .BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: requeue, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _dispatch.Release();
            }
        };

        await consume
            .BasicConsumeAsync(o.QueueName, autoAck: false, consumer, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Transponder RabbitMQ consuming queue {Queue}", o.QueueName);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            await DisposeCoreAsync().ConfigureAwait(false);
            _stateObserver.OnTransportConnectionStateChanged(TransportName, TransponderTransportConnectionState.Disconnected);
        }
        finally
        {
            _lifecycle.Release();
            _lifecycle.Dispose();
            _dispatch.Dispose();
        }
    }

    private static bool ComputeRequeueOnFailure(TransponderRabbitMqOptions o, bool deadLetterConfigured)
    {
        if (o.PoisonMessagePolicy == RabbitMqPoisonMessagePolicy.Requeue)
            return true;

        return !deadLetterConfigured;
    }

    /// <summary>
    /// Derives the exchange a message of <paramref name="messageType"/> is published on, by
    /// convention from its namespace (<c>Dialysis.&lt;Module&gt;.*</c> → <c>dialysis.{module}.events</c>).
    /// Falls back to <paramref name="localExchange"/> for types that don't fit the module convention
    /// (transport-internal chunk messages, shared-kernel events).
    /// </summary>
    private static string ResolveProducerExchange(Type messageType, string localExchange)
    {
        var ns = messageType.Namespace;
        if (ns is null || !ns.StartsWith("Dialysis.", StringComparison.Ordinal))
            return localExchange;

        var afterDialysis = ns.AsSpan("Dialysis.".Length);
        var dot = afterDialysis.IndexOf('.');
        var firstSegment = (dot < 0 ? afterDialysis : afterDialysis[..dot]).ToString();

        // Infra namespaces under Dialysis.* are not modules — they emit/consume locally.
        if (firstSegment is "BuildingBlocks" or "DomainDrivenDesign" or "CQRS" or "Module")
            return localExchange;

        return $"dialysis.{firstSegment.ToLowerInvariant()}.events";
    }

    private static IDictionary<string, object?>? BuildQueueArguments(TransponderRabbitMqOptions o, bool deadLetterConfigured)
    {
        var hasDeadLetter = deadLetterConfigured && o.PoisonMessagePolicy == RabbitMqPoisonMessagePolicy.DeadLetter;
        var isQuorum = o.QueueType == RabbitMqQueueType.Quorum;
        if (!hasDeadLetter && !isQuorum)
            return null;

        var args = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (hasDeadLetter)
            args["x-dead-letter-exchange"] = o.DeadLetterFanoutExchangeName!.Trim();
        if (isQuorum)
            args["x-queue-type"] = "quorum";
        return args;
    }

    private async Task DisposeCoreAsync()
    {
        if (_publishChannel is not null)
        {
            try
            {
                await _publishChannel.CloseAsync().ConfigureAwait(false);
                await _publishChannel.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error closing publish channel");
            }

            _publishChannel = null;
        }

        if (_consumeChannel is not null)
        {
            try
            {
                await _consumeChannel.CloseAsync().ConfigureAwait(false);
                await _consumeChannel.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error closing consume channel");
            }

            _consumeChannel = null;
        }

        if (_connection is not null)
        {
            try
            {
                await _connection.CloseAsync().ConfigureAwait(false);
                await _connection.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error closing connection");
            }

            _connection = null;
        }
    }

    private static BasicProperties CreateProps(TransportMessage message)
    {
        var correlation = message.CorrelationId ?? Guid.NewGuid().ToString("N");
        var messageId = string.IsNullOrEmpty(message.DeduplicationId) ? correlation : message.DeduplicationId;
        var props = new BasicProperties
        {
            ContentType = message.ContentType,
            CorrelationId = correlation,
            MessageId = messageId,
            DeliveryMode = DeliveryModes.Persistent,
        };

        if (message.Headers is { Count: > 0 })
        {
            props.Headers = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var kv in message.Headers)
                props.Headers[kv.Key] = kv.Value;
        }

        return props;
    }
}
