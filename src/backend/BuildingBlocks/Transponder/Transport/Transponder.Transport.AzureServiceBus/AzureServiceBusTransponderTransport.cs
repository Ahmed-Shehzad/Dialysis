using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Transport.AzureServiceBus;

/// <summary>
/// Publishes to a Service Bus topic and consumes a subscription; CLR routing key is carried in application properties (same names as <see cref="TransponderTransportHeaderNames"/>).
/// </summary>
public sealed class AzureServiceBusTransponderTransport : ITransponderTransport
{
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private ServiceBusClient? _client;
    private ServiceBusSender? _sender;
    private ServiceBusProcessor? _processor;
    private readonly IOptions<TransponderAzureServiceBusOptions> _options;
    private readonly ILogger<AzureServiceBusTransponderTransport> _logger;
    /// <summary>
    /// Publishes to a Service Bus topic and consumes a subscription; CLR routing key is carried in application properties (same names as <see cref="TransponderTransportHeaderNames"/>).
    /// </summary>
    public AzureServiceBusTransponderTransport(IOptions<TransponderAzureServiceBusOptions> options,
        ILogger<AzureServiceBusTransponderTransport> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client is not null && _sender is not null)
                return;

            await DisposeCoreAsync().ConfigureAwait(false);

            var o = _options.Value;
            if (string.IsNullOrWhiteSpace(o.ConnectionString))
                throw new InvalidOperationException("Transponder Azure Service Bus: ConnectionString is required.");
            if (string.IsNullOrWhiteSpace(o.TopicName))
                throw new InvalidOperationException("Transponder Azure Service Bus: TopicName is required.");
            if (string.IsNullOrWhiteSpace(o.SubscriptionName))
                throw new InvalidOperationException("Transponder Azure Service Bus: SubscriptionName is required.");

            _client = new ServiceBusClient(o.ConnectionString);
            _sender = _client.CreateSender(o.TopicName.Trim());
            _logger.LogInformation(
                "Transponder Azure Service Bus client ready for topic {Topic}, subscription {Subscription}",
                o.TopicName,
                o.SubscriptionName);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public async Task PublishAsync(TransportMessage message, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var sender = _sender ?? throw new InvalidOperationException("Service Bus sender is not initialized.");

        var correlation = message.CorrelationId ?? Guid.NewGuid().ToString("N");
        var deduplicationId = string.IsNullOrEmpty(message.DeduplicationId) ? correlation : message.DeduplicationId;

        var sbMessage = new ServiceBusMessage(BinaryData.FromBytes(message.Payload.ToArray()))
        {
            CorrelationId = correlation,
            MessageId = TruncateMessageId(deduplicationId),
            ContentType = message.ContentType ?? "application/json",
            Subject = message.RoutingKey,
        };

        sbMessage.ApplicationProperties[TransponderTransportHeaderNames.RoutingKey] = message.RoutingKey;
        if (!string.IsNullOrEmpty(message.CorrelationId))
            sbMessage.ApplicationProperties[TransponderTransportHeaderNames.CorrelationId] = message.CorrelationId!;
        if (!string.IsNullOrEmpty(message.DeduplicationId))
            sbMessage.ApplicationProperties[TransponderTransportHeaderNames.DeduplicationId] = message.DeduplicationId!;

        if (message.Headers is { Count: > 0 })
        {
            foreach (var kv in message.Headers)
                sbMessage.ApplicationProperties[kv.Key] = kv.Value;
        }

        await sender.SendMessageAsync(sbMessage, cancellationToken).ConfigureAwait(false);
    }

    public async Task RunConsumerAsync(
        Func<TransportMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onMessage);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        var o = _options.Value;
        var client = _client ?? throw new InvalidOperationException("Service Bus client is not initialized.");

        if (_processor is not null)
            throw new InvalidOperationException("Transponder Azure Service Bus: consumer already running.");

        _processor = client.CreateProcessor(o.TopicName.Trim(), o.SubscriptionName.Trim(), new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = Math.Max(1, o.MaxConcurrentCalls),
            PrefetchCount = o.PrefetchCount,
        });

        _processor.ProcessMessageAsync += args => ProcessOneAsync(args, onMessage);
        _processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "Transponder Azure Service Bus processor error: {Error}", args.ErrorSource);
            return Task.CompletedTask;
        };

        await _processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Transponder Azure Service Bus processor started on topic {Topic}, subscription {Subscription}",
            o.TopicName,
            o.SubscriptionName);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // shutdown
        }
        finally
        {
            try
            {
                await _processor.StopProcessingAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error stopping Service Bus processor");
            }

            await _processor.DisposeAsync().ConfigureAwait(false);
            _processor = null;
        }
    }

    private async Task ProcessOneAsync(
        ProcessMessageEventArgs args,
        Func<TransportMessage, CancellationToken, Task> onMessage)
    {
        var transport = ToTransportMessage(args.Message);
        if (transport is null)
        {
            _logger.LogWarning("Transponder Azure Service Bus: message {MessageId} missing routing key; completing", args.Message.MessageId);
            await args.CompleteMessageAsync(args.Message, args.CancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            await onMessage(transport.Value, args.CancellationToken).ConfigureAwait(false);
            await args.CompleteMessageAsync(args.Message, args.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transponder Azure Service Bus handler failed; abandoning message {MessageId}", args.Message.MessageId);
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken).ConfigureAwait(false);
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
        }
    }

    private async Task DisposeCoreAsync()
    {
        if (_processor is not null)
        {
            try
            {
                await _processor.StopProcessingAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }

            await _processor.DisposeAsync().ConfigureAwait(false);
            _processor = null;
        }

        if (_sender is not null)
        {
            await _sender.DisposeAsync().ConfigureAwait(false);
            _sender = null;
        }

        if (_client is not null)
        {
            await _client.DisposeAsync().ConfigureAwait(false);
            _client = null;
        }
    }

    private static TransportMessage? ToTransportMessage(ServiceBusReceivedMessage message)
    {
        var routingKey = GetApplicationPropertyString(message, TransponderTransportHeaderNames.RoutingKey);
        if (string.IsNullOrEmpty(routingKey))
            routingKey = message.Subject;

        if (string.IsNullOrEmpty(routingKey))
            return null;

        var correlationId = message.CorrelationId
            ?? GetApplicationPropertyString(message, TransponderTransportHeaderNames.CorrelationId);
        var deduplicationId = GetApplicationPropertyString(message, TransponderTransportHeaderNames.DeduplicationId)
            ?? message.MessageId;

        return new TransportMessage(
            routingKey,
            message.Body.ToMemory(),
            correlationId,
            message.ContentType ?? "application/json",
            DeduplicationId: deduplicationId);
    }

    private static string? GetApplicationPropertyString(ServiceBusReceivedMessage message, string key)
    {
        if (!message.ApplicationProperties.TryGetValue(key, out var value) || value is null)
            return null;

        return value switch
        {
            string s => s,
            byte[] b => Encoding.UTF8.GetString(b),
            _ => value.ToString(),
        };
    }

    /// <summary>Service Bus message id max length is 128.</summary>
    private static string TruncateMessageId(string id) =>
        id.Length <= 128 ? id : id[..128];
}
