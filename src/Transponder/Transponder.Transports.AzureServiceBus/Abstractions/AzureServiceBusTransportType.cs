namespace Transponder.Transports.AzureServiceBus.Abstractions;

/// <summary>
/// Defines transport protocol options for Azure Service Bus.
/// </summary>
public enum AzureServiceBusTransportType
{
    /// <summary>
    /// Uses AMQP over TCP.
    /// </summary>
    AmqpTcp,

    /// <summary>
    /// Uses AMQP over WebSockets.
    /// </summary>
    AmqpWebSockets
}
