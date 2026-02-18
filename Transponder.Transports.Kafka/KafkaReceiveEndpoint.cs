using Confluent.Kafka;

using Polly;

using Transponder.Transports.Abstractions;
using Transponder.Transports.Kafka.Abstractions;

namespace Transponder.Transports.Kafka;

internal sealed class KafkaReceiveEndpoint : IReceiveEndpoint
{
    private readonly Func<IReceiveContext, Task> _handler;
    private readonly IKafkaHostSettings _settings;
    private readonly Uri _inputAddress;
    private readonly Uri _hostAddress;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly KafkaSendTransport? _deadLetterTransport;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public KafkaReceiveEndpoint(
        IReceiveEndpointConfiguration configuration,
        IKafkaHostSettings settings,
        Uri hostAddress,
        IProducer<string, byte[]> producer,
        ReceiveEndpointFaultSettings? faultSettings,
        ResiliencePipeline resiliencePipeline)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(hostAddress);
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(resiliencePipeline);

        _handler = configuration.Handler ?? throw new ArgumentNullException(nameof(configuration));
        _inputAddress = configuration.InputAddress;
        _settings = settings;
        _hostAddress = hostAddress;
        _resiliencePipeline = resiliencePipeline;
        _deadLetterTransport = faultSettings?.DeadLetterAddress is null
            ? null
            : new KafkaSendTransport(producer, settings.Topology.GetTopicName(faultSettings.DeadLetterAddress));
    }

    public Uri InputAddress => _inputAddress;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_loop is not null) return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loop = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is null) return;

        await _cts.CancelAsync();
        if (_loop is not null) await _loop.ConfigureAwait(false);

        _cts.Dispose();
        _cts = null;
        _loop = null;
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        string topic = _settings.Topology.GetTopicName(_inputAddress);
        string groupId = _settings.Topology.GetConsumerGroup(_inputAddress);
        ConsumerConfig config = KafkaTransportHost.BuildConsumerConfig(_settings, groupId);

        using IConsumer<string, byte[]>? consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        consumer.Subscribe(topic);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ConsumeResult<string, byte[]>? result = consumer.Consume(cancellationToken);
                if (result?.Message is null) continue;

                var transportMessage = CreateTransportMessage(result);
                var context = new KafkaReceiveContext(
                    transportMessage,
                    _hostAddress,
                    _inputAddress,
                    cancellationToken);

                await ProcessMessageAsync(transportMessage, context, result, consumer, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            consumer.Close();
        }
    }

    private static TransportMessage CreateTransportMessage(ConsumeResult<string, byte[]> result)
    {
        var headers = KafkaTransportHeaders.ReadHeaders(result.Message.Headers);
        var parsed = ParseMessageHeaders(headers);
        var messageId = Ulid.TryParse(result.Message.Key, out Ulid mid) ? mid : (Ulid?)null;
        return new TransportMessage(
            result.Message.Value ?? [],
            parsed.ContentType,
            headers,
            new MessageIdentity(messageId, parsed.CorrelationId, parsed.ConversationId),
            parsed.MessageType);
    }

    private static (string? ContentType, string? MessageType, Ulid? CorrelationId, Ulid? ConversationId) ParseMessageHeaders(
        Dictionary<string, object?> headers)
    {
        string? contentType = headers.TryGetValue("ContentType", out object? ct) ? ct as string : null;
        _ = headers.Remove("ContentType");
        string? messageType = headers.TryGetValue("MessageType", out object? mt) ? mt as string : null;
        _ = headers.Remove("MessageType");
        Ulid? correlationId = headers.TryGetValue("CorrelationId", out object? corr)
                              && Ulid.TryParse(corr?.ToString(), out Ulid parsedCorrelationId)
            ? parsedCorrelationId
            : (Ulid?)null;
        _ = headers.Remove("CorrelationId");
        Ulid? conversationId = headers.TryGetValue("ConversationId", out object? conv)
                               && Ulid.TryParse(conv?.ToString(), out Ulid parsedConversationId)
            ? parsedConversationId
            : (Ulid?)null;
        _ = headers.Remove("ConversationId");
        return (contentType, messageType, correlationId, conversationId);
    }

    private async Task ProcessMessageAsync(
        TransportMessage transportMessage,
        KafkaReceiveContext context,
        ConsumeResult<string, byte[]> result,
        IConsumer<string, byte[]> consumer,
        CancellationToken cancellationToken)
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(
                    async _ => await _handler(context).ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);
            consumer.Commit(result);
            return;
        }
        catch
        {
            if (_deadLetterTransport is null) return;
        }

        try
        {
            await _deadLetterTransport.SendAsync(transportMessage, cancellationToken)
                .ConfigureAwait(false);
            consumer.Commit(result);
        }
        catch
        {
            // Leave offset uncommitted for retry if DLQ dispatch fails.
        }
    }
}
