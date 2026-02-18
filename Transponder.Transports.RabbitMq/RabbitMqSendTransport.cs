using RabbitMQ.Client;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.RabbitMq;

internal sealed class RabbitMqSendTransport : ISendTransport
{
    private readonly IConnection _connection;
    private readonly string _queueName;

    public RabbitMqSendTransport(IConnection connection, string queueName)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
    }

    public async Task SendAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await using IChannel channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        _ = await channel.QueueDeclareAsync(
                _queueName,
                durable: true,
                exclusive: false,
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
                exchange: string.Empty,
                routingKey: _queueName,
                mandatory: false,
                basicProperties: properties,
                body: message.Body,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
