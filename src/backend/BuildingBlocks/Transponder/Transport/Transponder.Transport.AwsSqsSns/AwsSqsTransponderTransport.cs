using System.Text;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Transport.AwsSqsSns;

/// <summary>
/// Publishes and consumes a single standard SQS queue; CLR routing key is stored in string message attributes.
/// </summary>
public sealed class AwsSqsTransponderTransport : ITransponderTransport
{
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private AmazonSQSClient? _sqs;
    private readonly IOptions<TransponderAwsSqsOptions> _options;
    private readonly ILogger<AwsSqsTransponderTransport> _logger;
    /// <summary>
    /// Publishes and consumes a single standard SQS queue; CLR routing key is stored in string message attributes.
    /// </summary>
    public AwsSqsTransponderTransport(IOptions<TransponderAwsSqsOptions> options,
        ILogger<AwsSqsTransponderTransport> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async ValueTask EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_sqs is not null)
                return;

            var o = _options.Value;
            if (string.IsNullOrWhiteSpace(o.QueueUrl))
                throw new InvalidOperationException("Transponder AWS SQS: QueueUrl is required.");

            _sqs = CreateClient(o);
            _logger.LogInformation("Transponder AWS SQS client ready for queue {QueueUrl}", o.QueueUrl.Trim());
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    public async Task PublishAsync(TransportMessage message, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var sqs = _sqs ?? throw new InvalidOperationException("SQS client is not initialized.");
        var o = _options.Value;

        var correlation = message.CorrelationId ?? Guid.NewGuid().ToString("N");
        var deduplicationId = string.IsNullOrEmpty(message.DeduplicationId) ? correlation : message.DeduplicationId;

        var attrs = new Dictionary<string, MessageAttributeValue>(StringComparer.Ordinal)
        {
            [TransponderTransportHeaderNames.RoutingKey] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = message.RoutingKey,
            },
        };

        if (!string.IsNullOrEmpty(message.CorrelationId))
        {
            attrs[TransponderTransportHeaderNames.CorrelationId] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = message.CorrelationId!,
            };
        }

        if (!string.IsNullOrEmpty(message.DeduplicationId))
        {
            attrs[TransponderTransportHeaderNames.DeduplicationId] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = message.DeduplicationId!,
            };
        }

        var body = Encoding.UTF8.GetString(message.Payload.Span);
        var request = new SendMessageRequest
        {
            QueueUrl = o.QueueUrl.Trim(),
            MessageBody = body,
            MessageAttributes = attrs,
        };

        await sqs.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task RunConsumerAsync(
        Func<TransportMessage, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onMessage);
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        var sqs = _sqs ?? throw new InvalidOperationException("SQS client is not initialized.");
        var o = _options.Value;
        var queueUrl = o.QueueUrl.Trim();
        var wait = Math.Clamp(o.WaitTimeSeconds, 0, 20);
        var maxMsgs = Math.Clamp(o.MaxNumberOfMessages, 1, 10);

        _logger.LogInformation("Transponder AWS SQS receive loop started for {QueueUrl}", queueUrl);

        while (!cancellationToken.IsCancellationRequested)
        {
            ReceiveMessageResponse response;
            try
            {
                response = await sqs
                    .ReceiveMessageAsync(
                        new ReceiveMessageRequest
                        {
                            QueueUrl = queueUrl,
                            MaxNumberOfMessages = maxMsgs,
                            WaitTimeSeconds = wait,
                            MessageAttributeNames = ["All"],
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            foreach (var m in response.Messages)
            {
                var transport = ToTransportMessage(m);
                if (transport is null)
                {
                    _logger.LogWarning("Transponder AWS SQS: message {Id} missing routing key; deleting", m.MessageId);
                    await TryDeleteAsync(sqs, queueUrl, m, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                try
                {
                    await onMessage(transport.Value, cancellationToken).ConfigureAwait(false);
                    await sqs.DeleteMessageAsync(queueUrl, m.ReceiptHandle, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Transponder AWS SQS handler failed; leaving message {Id} for redelivery",
                        m.MessageId);
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_sqs is not null)
            {
                _sqs.Dispose();
                _sqs = null;
            }
        }
        finally
        {
            _lifecycle.Release();
            _lifecycle.Dispose();
        }
    }

    private static AmazonSQSClient CreateClient(TransponderAwsSqsOptions o)
    {
        if (!string.IsNullOrWhiteSpace(o.RegionName))
            return new AmazonSQSClient(RegionEndpoint.GetBySystemName(o.RegionName.Trim()));

        return new AmazonSQSClient();
    }

    private static TransportMessage? ToTransportMessage(Message message)
    {
        var routingKey = GetStringAttribute(message, TransponderTransportHeaderNames.RoutingKey);
        if (string.IsNullOrEmpty(routingKey))
            return null;

        var correlationId = GetStringAttribute(message, TransponderTransportHeaderNames.CorrelationId);
        var deduplicationId = GetStringAttribute(message, TransponderTransportHeaderNames.DeduplicationId);

        var bytes = Encoding.UTF8.GetBytes(message.Body ?? string.Empty);
        return new TransportMessage(
            routingKey,
            bytes,
            correlationId,
            ContentType: "application/json",
            DeduplicationId: string.IsNullOrEmpty(deduplicationId) ? message.MessageId : deduplicationId);
    }

    private static string? GetStringAttribute(Message message, string name)
    {
        if (!message.MessageAttributes.TryGetValue(name, out var attr) || attr.StringValue is null)
            return null;

        return attr.StringValue;
    }

    private async Task TryDeleteAsync(IAmazonSQS sqs, string queueUrl, Message m, CancellationToken ct)
    {
        try
        {
            await sqs.DeleteMessageAsync(queueUrl, m.ReceiptHandle, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Transponder AWS SQS: failed to delete message {Id}", m.MessageId);
        }
    }
}
