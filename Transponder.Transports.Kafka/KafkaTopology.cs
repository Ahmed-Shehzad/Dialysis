using Transponder.Transports.Kafka.Abstractions;

namespace Transponder.Transports.Kafka;

/// <summary>
/// Default Kafka topology conventions.
/// </summary>
public sealed class KafkaTopology : IKafkaTopology
{
    public string GetTopicName(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        return messageType.Name;
    }

    public string GetTopicName(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return !string.IsNullOrWhiteSpace(address.AbsolutePath) && address.AbsolutePath != "/"
            ? address.AbsolutePath.Trim('/')
            : address.Host;
    }

    public string GetConsumerGroup(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return $"{address.Host}-consumer";
    }
}
