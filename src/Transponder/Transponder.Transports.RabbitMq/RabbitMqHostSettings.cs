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
        string host,
        IRabbitMqTopology? topology = null,
        int port = 5672,
        string virtualHost = "/",
        bool useTls = false,
        string? username = null,
        string? password = null,
        TimeSpan? requestedHeartbeat = null,
        IReadOnlyDictionary<string, object?>? settings = null,
        TransportResilienceOptions? resilienceOptions = null)
        : base(address, settings, resilienceOptions)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Host must be provided.", nameof(host));

        Host = host;
        Topology = topology ?? new RabbitMqTopology();
        Port = port;
        VirtualHost = virtualHost;
        UseTls = useTls;
        Username = username;
        Password = password;
        RequestedHeartbeat = requestedHeartbeat;
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
