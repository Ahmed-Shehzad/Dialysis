using Transponder.Transports.RabbitMq.Abstractions;

namespace Transponder.Transports.RabbitMq;

/// <summary>
/// Default RabbitMQ topology conventions.
/// </summary>
public sealed class RabbitMqTopology : IRabbitMqTopology
{
    public string ExchangeType => "fanout";

    public string GetExchangeName(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        return messageType.Name;
    }

    public string GetRoutingKey(Type messageType) => GetExchangeName(messageType);

    public string GetQueueName(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return !string.IsNullOrWhiteSpace(address.AbsolutePath) && address.AbsolutePath != "/"
            ? address.AbsolutePath.Trim('/')
            : address.Host;
    }
}
