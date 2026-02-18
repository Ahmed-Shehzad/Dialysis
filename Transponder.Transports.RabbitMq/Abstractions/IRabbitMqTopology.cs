namespace Transponder.Transports.RabbitMq.Abstractions;

/// <summary>
/// Defines RabbitMQ topology conventions.
/// </summary>
public interface IRabbitMqTopology
{
    /// <summary>
    /// Gets the exchange name for a message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    string GetExchangeName(Type messageType);

    /// <summary>
    /// Gets the exchange type to use for published messages.
    /// </summary>
    string ExchangeType { get; }

    /// <summary>
    /// Gets the routing key for a message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    string GetRoutingKey(Type messageType);

    /// <summary>
    /// Gets the queue name for a destination address.
    /// </summary>
    /// <param name="address">The destination address.</param>
    string GetQueueName(Uri address);
}
