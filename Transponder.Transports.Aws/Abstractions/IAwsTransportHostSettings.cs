using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Aws.Abstractions;

/// <summary>
/// Provides AWS-specific settings for creating a transport host.
/// </summary>
public interface IAwsTransportHostSettings : ITransportHostSettings
{
    /// <summary>
    /// Gets the AWS topology conventions.
    /// </summary>
    IAwsTopology Topology { get; }

    /// <summary>
    /// Gets the AWS region name.
    /// </summary>
    string Region { get; }

    /// <summary>
    /// Gets the AWS access key identifier, if explicitly configured.
    /// </summary>
    string? AccessKeyId { get; }

    /// <summary>
    /// Gets the AWS secret access key, if explicitly configured.
    /// </summary>
    string? SecretAccessKey { get; }

    /// <summary>
    /// Gets the AWS session token, if explicitly configured.
    /// </summary>
    string? SessionToken { get; }

    /// <summary>
    /// Gets the service URL override (useful for local endpoints).
    /// </summary>
    string? ServiceUrl { get; }

    /// <summary>
    /// Gets whether TLS is required for the transport.
    /// </summary>
    bool UseTls { get; }
}
