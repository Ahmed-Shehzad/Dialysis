using Transponder.Transports.Abstractions;

namespace Transponder.Transports.AzureServiceBus.Abstractions;

/// <summary>
/// Provides Azure Service Bus specific settings for creating a transport host.
/// </summary>
public interface IAzureServiceBusHostSettings : ITransportHostSettings
{
    /// <summary>
    /// Gets the Azure Service Bus topology conventions.
    /// </summary>
    IAzureServiceBusTopology Topology { get; }

    /// <summary>
    /// Gets the connection string, if configured.
    /// </summary>
    string? ConnectionString { get; }

    /// <summary>
    /// Gets the fully qualified namespace, if configured (e.g. "mybus.servicebus.windows.net").
    /// </summary>
    string? FullyQualifiedNamespace { get; }

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
