using RabbitMQ.Client;

using Transponder.Transports.Abstractions;
using Transponder.Transports.RabbitMq.Abstractions;

namespace Transponder.Transports.RabbitMq;

internal sealed class RabbitMqPublishTransport : IPublishTransport
{
    private readonly IConnection _connection;
    private readonly string _exchangeName;
    private readonly IRabbitMqTopology _topology;
    private readonly Type _messageType;

    public RabbitMqPublishTransport(
        IConnection connection,
        string exchangeName,
        IRabbitMqTopology topology,
        Type messageType)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _exchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
        _topology = topology ?? throw new ArgumentNullException(nameof(topology));
        _messageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
    }

    public async Task PublishAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await using IChannel channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(
                _exchangeName,
                _topology.ExchangeType,
                durable: true,
                autoDelete: false,
                arguments: null,
                noWait: false,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var properties = new BasicProperties
        {
            ContentType = message.ContentType,
            MessageId = message.MessageId?.ToString(),
            CorrelationId = message.CorrelationId?.ToString(),
            Headers = RabbitMqTransportHeaders.BuildHeaders(message)
        };

        await channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: _topology.GetRoutingKey(_messageType),
                mandatory: false,
                basicProperties: properties,
                body: message.Body,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
