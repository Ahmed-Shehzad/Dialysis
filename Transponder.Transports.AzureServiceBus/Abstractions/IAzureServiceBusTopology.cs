namespace Transponder.Transports.AzureServiceBus.Abstractions;

/// <summary>
/// Defines Azure Service Bus topology conventions.
/// </summary>
public interface IAzureServiceBusTopology
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

    /// <summary>
    /// Gets the subscription name for a destination address, if any.
    /// </summary>
    /// <param name="address">The destination address.</param>
    string? GetSubscriptionName(Uri address);
}
