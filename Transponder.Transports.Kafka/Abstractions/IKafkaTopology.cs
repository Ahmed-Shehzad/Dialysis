namespace Transponder.Transports.Kafka.Abstractions;

/// <summary>
/// Defines Kafka topology conventions.
/// </summary>
public interface IKafkaTopology
{
    /// <summary>
    /// Gets the topic name for a message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    string GetTopicName(Type messageType);

    /// <summary>
    /// Gets the topic name for a destination address.
    /// </summary>
    /// <param name="address">The destination address.</param>
    string GetTopicName(Uri address);

    /// <summary>
    /// Gets the consumer group identifier for a destination address.
    /// </summary>
    /// <param name="address">The destination address.</param>
    string GetConsumerGroup(Uri address);
}
