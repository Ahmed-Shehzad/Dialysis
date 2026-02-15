using Transponder.Transports.Abstractions;
using Transponder.Transports.AzureServiceBus.Abstractions;

namespace Transponder.Transports.AzureServiceBus;

/// <summary>
/// Default Azure Service Bus transport host settings.
/// </summary>
public sealed class AzureServiceBusHostSettings : TransportHostSettings, IAzureServiceBusHostSettings
{
    public AzureServiceBusHostSettings(
        Uri address,
        IAzureServiceBusTopology? topology = null,
        AzureServiceBusTransportType transportType = AzureServiceBusTransportType.AmqpTcp,
        string? connectionString = null,
        string? fullyQualifiedNamespace = null,
        string? sharedAccessKeyName = null,
        string? sharedAccessKey = null,
        IReadOnlyDictionary<string, object?>? settings = null,
        TransportResilienceOptions? resilienceOptions = null)
        : base(address, settings, resilienceOptions)
    {
        Topology = topology ?? new AzureServiceBusTopology();
        TransportType = transportType;
        ConnectionString = connectionString;
        FullyQualifiedNamespace = fullyQualifiedNamespace;
        SharedAccessKeyName = sharedAccessKeyName;
        SharedAccessKey = sharedAccessKey;
    }

    public IAzureServiceBusTopology Topology { get; }

    public string? ConnectionString { get; }

    public string? FullyQualifiedNamespace { get; }

    public string? SharedAccessKeyName { get; }

    public string? SharedAccessKey { get; }

    public AzureServiceBusTransportType TransportType { get; }
}
