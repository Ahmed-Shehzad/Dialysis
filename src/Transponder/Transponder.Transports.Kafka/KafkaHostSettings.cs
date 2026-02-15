using Transponder.Transports.Abstractions;
using Transponder.Transports.Kafka.Abstractions;

namespace Transponder.Transports.Kafka;

/// <summary>
/// Default Kafka transport host settings.
/// </summary>
public sealed class KafkaHostSettings : TransportHostSettings, IKafkaHostSettings
{
    public KafkaHostSettings(
        Uri address,
        IReadOnlyList<string> bootstrapServers,
        IKafkaTopology? topology = null,
        string? clientId = null,
        string? securityProtocol = null,
        string? saslMechanism = null,
        string? saslUsername = null,
        string? saslPassword = null,
        IReadOnlyDictionary<string, object?>? settings = null,
        TransportResilienceOptions? resilienceOptions = null)
        : base(address, settings, resilienceOptions)
    {
        ArgumentNullException.ThrowIfNull(bootstrapServers);

        if (bootstrapServers.Count == 0) throw new ArgumentException("At least one bootstrap server is required.", nameof(bootstrapServers));

        BootstrapServers = bootstrapServers;
        Topology = topology ?? new KafkaTopology();
        ClientId = clientId;
        SecurityProtocol = securityProtocol;
        SaslMechanism = saslMechanism;
        SaslUsername = saslUsername;
        SaslPassword = saslPassword;
    }

    public IKafkaTopology Topology { get; }

    public IReadOnlyList<string> BootstrapServers { get; }

    public string? ClientId { get; }

    public string? SecurityProtocol { get; }

    public string? SaslMechanism { get; }

    public string? SaslUsername { get; }

    public string? SaslPassword { get; }
}
