using Transponder.Transports.Abstractions;

namespace Transponder.Transports.RabbitMq.Abstractions;

/// <summary>
/// Provides RabbitMQ specific settings for creating a transport host.
/// </summary>
public interface IRabbitMqHostSettings : ITransportHostSettings
{
    /// <summary>
    /// Gets the RabbitMQ topology conventions.
    /// </summary>
    IRabbitMqTopology Topology { get; }

    /// <summary>
    /// Gets the RabbitMQ host name.
    /// </summary>
    string Host { get; }

    /// <summary>
    /// Gets the RabbitMQ port.
    /// </summary>
    int Port { get; }

    /// <summary>
    /// Gets the virtual host.
    /// </summary>
    string VirtualHost { get; }

    /// <summary>
    /// Gets the user name, if configured.
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// Gets the password, if configured.
    /// </summary>
    string? Password { get; }

    /// <summary>
    /// Gets whether TLS is required for the transport.
    /// </summary>
    bool UseTls { get; }

    /// <summary>
    /// Gets the requested heartbeat interval, if configured.
    /// </summary>
    TimeSpan? RequestedHeartbeat { get; }
}
