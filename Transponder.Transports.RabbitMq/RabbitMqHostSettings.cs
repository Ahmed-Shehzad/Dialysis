using Transponder.Transports.Abstractions;
using Transponder.Transports.RabbitMq.Abstractions;

namespace Transponder.Transports.RabbitMq;

/// <summary>
/// Default RabbitMQ transport host settings.
/// </summary>
public sealed class RabbitMqHostSettings : TransportHostSettings, IRabbitMqHostSettings
{
    public RabbitMqHostSettings(
        Uri address,
        RabbitMqConnectionSettings connection,
        IRabbitMqTopology? topology = null,
        IReadOnlyDictionary<string, object?>? settings = null,
        TransportResilienceOptions? resilienceOptions = null)
        : base(address, settings, resilienceOptions)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (string.IsNullOrWhiteSpace(connection.Host)) throw new ArgumentException("Host must be provided.", nameof(connection));

        Host = connection.Host;
        Topology = topology ?? new RabbitMqTopology();
        Port = connection.Port;
        VirtualHost = connection.VirtualHost;
        UseTls = connection.UseTls;
        Username = connection.Username;
        Password = connection.Password;
        RequestedHeartbeat = connection.RequestedHeartbeat;
    }

    public IRabbitMqTopology Topology { get; }

    public string Host { get; }

    public int Port { get; }

    public string VirtualHost { get; }

    public string? Username { get; }

    public string? Password { get; }

    public bool UseTls { get; }

    public TimeSpan? RequestedHeartbeat { get; }
}
