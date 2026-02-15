using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Kafka.Abstractions;

/// <summary>
/// Provides Kafka specific settings for creating a transport host.
/// </summary>
public interface IKafkaHostSettings : ITransportHostSettings
{
    /// <summary>
    /// Gets the Kafka topology conventions.
    /// </summary>
    IKafkaTopology Topology { get; }

    /// <summary>
    /// Gets the bootstrap servers for the Kafka cluster.
    /// </summary>
    IReadOnlyList<string> BootstrapServers { get; }

    /// <summary>
    /// Gets the client identifier, if configured.
    /// </summary>
    string? ClientId { get; }

    /// <summary>
    /// Gets the security protocol, if configured (e.g. "SaslSsl").
    /// </summary>
    string? SecurityProtocol { get; }

    /// <summary>
    /// Gets the SASL mechanism, if configured (e.g. "Plain", "ScramSha256").
    /// </summary>
    string? SaslMechanism { get; }

    /// <summary>
    /// Gets the SASL user name, if configured.
    /// </summary>
    string? SaslUsername { get; }

    /// <summary>
    /// Gets the SASL password, if configured.
    /// </summary>
    string? SaslPassword { get; }
}
