namespace Transponder.Transports.Aws.Abstractions;

/// <summary>
/// Defines AWS topology conventions for queues and topics.
/// </summary>
public interface IAwsTopology
{
    /// <summary>
    /// Gets the queue name for a destination address.
    /// </summary>
    /// <param name="address">The destination address.</param>
    string GetQueueName(Uri address);

    /// <summary>
    /// Gets the topic name for a message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    string GetTopicName(Type messageType);
}
