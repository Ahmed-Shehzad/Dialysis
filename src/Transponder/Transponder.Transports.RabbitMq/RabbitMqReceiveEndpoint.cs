using Polly;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Transponder.Transports.Abstractions;
using Transponder.Transports.RabbitMq.Abstractions;

namespace Transponder.Transports.RabbitMq;

internal sealed class RabbitMqReceiveEndpoint : IReceiveEndpoint
{
    private readonly Func<CancellationToken, Task<IConnection>> _connectionFactory;
    private readonly Func<IReceiveContext, Task> _handler;
    private readonly string _queueName;
    private readonly Uri _inputAddress;
    private readonly Uri _hostAddress;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly string? _deadLetterQueueName;
    private IChannel? _channel;
    private string? _consumerTag;

    public RabbitMqReceiveEndpoint(
        Func<CancellationToken, Task<IConnection>> connectionFactory,
        IReceiveEndpointConfiguration configuration,
        IRabbitMqTopology topology,
        Uri hostAddress,
        ReceiveEndpointFaultSettings? faultSettings,
        ResiliencePipeline resiliencePipeline)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(hostAddress);
        ArgumentNullException.ThrowIfNull(resiliencePipeline);

        _inputAddress = configuration.InputAddress;
        _handler = configuration.Handler ?? throw new ArgumentNullException(nameof(configuration.Handler));
        _queueName = topology.GetQueueName(_inputAddress);
        _hostAddress = hostAddress;
        _resiliencePipeline = resiliencePipeline;
        _deadLetterQueueName = faultSettings?.DeadLetterAddress is null
            ? null
            : topology.GetQueueName(faultSettings.DeadLetterAddress);
    }

    public Uri InputAddress => _inputAddress;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_channel is not null) return;

        var connection = await _connectionFactory(cancellationToken).ConfigureAwait(false);
        _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        IDictionary<string, object?>? arguments = null;

        if (!string.IsNullOrWhiteSpace(_deadLetterQueueName))
        {
            _ = await _channel.QueueDeclareAsync(
                    _deadLetterQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    noWait: false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            arguments = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = string.Empty,
                ["x-dead-letter-routing-key"] = _deadLetterQueueName
            };
        }

        _ = await _channel.QueueDeclareAsync(
                _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments,
                noWait: false,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await _channel.BasicQosAsync(0, 1, false, cancellationToken).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        _consumerTag = await _channel.BasicConsumeAsync(
                _queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_channel is null) return;

        if (!string.IsNullOrWhiteSpace(_consumerTag)) await _channel.BasicCancelAsync(_consumerTag, false, cancellationToken).ConfigureAwait(false);

        await _channel.CloseAsync(cancellationToken).ConfigureAwait(false);
        _channel.Dispose();
        _channel = null;
        _consumerTag = null;
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null) return;

        var headers = RabbitMqTransportHeaders.ReadHeaders(args.BasicProperties.Headers);
        var contentType = args.BasicProperties.ContentType;
        var messageType = headers.TryGetValue("MessageType", out var typeValue) ? typeValue as string : null;
        _ = headers.Remove("MessageType");
        var conversationId = headers.TryGetValue("ConversationId", out var conv)
                             && Ulid.TryParse(conv?.ToString(), out var parsedConversationId)
            ? parsedConversationId
            : (Ulid?)null;
        _ = headers.Remove("ConversationId");

        var transportMessage = new TransportMessage(
            args.Body.ToArray(),
            contentType,
            headers,
            Ulid.TryParse(args.BasicProperties.MessageId, out var messageId) ? messageId : null,
            Ulid.TryParse(args.BasicProperties.CorrelationId, out var correlationId) ? correlationId : null,
            conversationId,
            messageType,
            null);

        var context = new RabbitMqReceiveContext(
            transportMessage,
            _hostAddress,
            _inputAddress,
            CancellationToken.None);

        try
        {
            await _resiliencePipeline.ExecuteAsync(
                    async ct => await _handler(context).ConfigureAwait(false),
                    CancellationToken.None)
                .ConfigureAwait(false);
            await _channel.BasicAckAsync(args.DeliveryTag, false, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(_deadLetterQueueName)) await _channel.BasicRejectAsync(args.DeliveryTag, false, CancellationToken.None).ConfigureAwait(false);
            else await _channel.BasicNackAsync(args.DeliveryTag, false, true, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
