using Azure.Core;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.AzureServiceBus.Abstractions;

/// <summary>
/// Provides Azure Service Bus specific settings for creating a transport host.
/// Supports connection string auth or passwordless (TokenCredential + namespace).
/// </summary>
public interface IAzureServiceBusHostSettings : ITransportHostSettings
{
    /// <summary>
    /// Gets the Azure Service Bus topology conventions.
    /// </summary>
    IAzureServiceBusTopology Topology { get; }

    /// <summary>
    /// Gets the connection string, if configured. When set, takes precedence over passwordless.
    /// </summary>
    string? ConnectionString { get; }

    /// <summary>
    /// Gets the fully qualified namespace (e.g. "mybus.servicebus.windows.net").
    /// Required for passwordless auth when <see cref="ConnectionString"/> is null.
    /// </summary>
    string? FullyQualifiedNamespace { get; }

    /// <summary>
    /// Gets the token credential for passwordless auth. Used with <see cref="FullyQualifiedNamespace"/>
    /// when <see cref="ConnectionString"/> is null. Typically <see cref="Azure.Identity.DefaultAzureCredential"/>.
    /// </summary>
    TokenCredential? Credential { get; }

    /// <summary>
    /// Gets the shared access key name, if configured.
    /// </summary>
    string? SharedAccessKeyName { get; }

    /// <summary>
    /// Gets the shared access key, if configured.
    /// </summary>
    string? SharedAccessKey { get; }

    /// <summary>
    /// Gets the transport protocol selection.
    /// </summary>
    AzureServiceBusTransportType TransportType { get; }
}
