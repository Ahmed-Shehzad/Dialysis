using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;

/// <summary>
/// RabbitMQ <see cref="ITransponderTransport"/> using separate publish and consumer channels.
/// </summary>
public sealed class RabbitMqTransponderTransport(
    IOptions<TransponderRabbitMqOptions> options,
    RabbitMqSubscriptionRegistry registry,
    ILogger<RabbitMqTransponderTransport> logger) : ITransponderTransport
{
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private readonly SemaphoreSlim _dispatch = new(1, 1);
    private IConnection? _connection;
    private IChannel? _publishChannel;
    private IChannel? _consumeChannel;

    public async ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection is { IsOpen: true } && _publishChannel is { IsOpen: true } && _consumeChannel is { IsOpen: true })
                return;

            await DisposeCoreAsync().ConfigureAwait(false);

            var o = options.Value;
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

            logger.LogInformation(
                "Transponder RabbitMQ connected; exchange {Exchange}, publisher confirms {Confirms}",
                o.ExchangeName,
                o.PublisherConfirmsEnabled);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public async Task PublishAsync(TransportMessage message, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var o = options.Value;
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

        var o = options.Value;
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

            logger.LogInformation(
                "Transponder RabbitMQ dead-letter fanout {Dlx} bound to queue {Dlq}",
                dlx,
                dlq);
        }
        else if (o.PoisonMessagePolicy == RabbitMqPoisonMessagePolicy.DeadLetter)
        {
            logger.LogWarning(
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

        foreach (var routingKey in registry.RoutingKeys)
        {
            await consume
                .QueueBindAsync(
                    queue: o.QueueName,
                    exchange: o.ExchangeName,
                    routingKey: routingKey,
                    arguments: null,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
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
                logger.LogError(ex, "Transponder consumer handler failed; nack requeue={Requeue}", requeue);
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

        logger.LogInformation("Transponder RabbitMQ consuming queue {Queue}", o.QueueName);

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

    private static IDictionary<string, object?>? BuildQueueArguments(TransponderRabbitMqOptions o, bool deadLetterConfigured)
    {
        if (!deadLetterConfigured || o.PoisonMessagePolicy != RabbitMqPoisonMessagePolicy.DeadLetter)
            return null;

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["x-dead-letter-exchange"] = o.DeadLetterFanoutExchangeName!.Trim(),
        };
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
                logger.LogDebug(ex, "Error closing publish channel");
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
                logger.LogDebug(ex, "Error closing consume channel");
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
                logger.LogDebug(ex, "Error closing connection");
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
