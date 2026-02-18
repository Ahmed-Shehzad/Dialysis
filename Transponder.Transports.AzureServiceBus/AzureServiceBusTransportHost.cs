using Azure.Messaging.ServiceBus;

using Polly;

using Transponder.Transports.Abstractions;
using Transponder.Transports.AzureServiceBus.Abstractions;

namespace Transponder.Transports.AzureServiceBus;

/// <summary>
/// Azure Service Bus transport host backed by Service Bus clients.
/// </summary>
public sealed class AzureServiceBusTransportHost : TransportHostBase
{
    private readonly ServiceBusClient _client;
    private readonly List<AzureServiceBusReceiveEndpoint> _receiveEndpoints = [];
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly TransportResilienceOptions? _resilienceOptions;

    public AzureServiceBusTransportHost(IAzureServiceBusHostSettings settings)
        : base(settings?.Address ?? throw new ArgumentNullException(nameof(settings)))
    {
        Settings = settings;
        _resilienceOptions = (settings as ITransportHostResilienceSettings)?.ResilienceOptions;
        _resiliencePipeline = TransportResiliencePipeline.Create(_resilienceOptions);
        if (string.IsNullOrWhiteSpace(settings.ConnectionString)) throw new InvalidOperationException("Azure Service Bus connection string must be provided.");

        var options = new ServiceBusClientOptions
        {
            TransportType = settings.TransportType == AzureServiceBusTransportType.AmqpWebSockets
                ? ServiceBusTransportType.AmqpWebSockets
                : ServiceBusTransportType.AmqpTcp
        };

        _client = new ServiceBusClient(settings.ConnectionString, options);
    }

    public IAzureServiceBusHostSettings Settings { get; }

    public override Task<ISendTransport> GetSendTransportAsync(
        Uri address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        var entityPath = AzureServiceBusEntityAddress.Parse(address, Settings.Topology);
        var transport = new AzureServiceBusSendTransport(_client, entityPath.EntityPath);
        ISendTransport resilientTransport = TransportResiliencePipeline.WrapSend(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override Task<IPublishTransport> GetPublishTransportAsync(
        Type messageType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        string topicName = Settings.Topology.GetTopicName(messageType);
        var transport = new AzureServiceBusPublishTransport(_client, topicName);
        IPublishTransport resilientTransport = TransportResiliencePipeline.WrapPublish(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override IReceiveEndpoint ConnectReceiveEndpoint(IReceiveEndpointConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ReceiveEndpointFaultSettings? faultSettings = ReceiveEndpointFaultSettingsResolver.Resolve(configuration);
        ResiliencePipeline pipeline = TransportResiliencePipeline.Create(faultSettings?.ResilienceOptions ?? _resilienceOptions);
        var endpoint = new AzureServiceBusReceiveEndpoint(
            _client,
            configuration,
            Settings.Topology,
            Address,
            faultSettings,
            pipeline);
        _receiveEndpoints.Add(endpoint);
        return endpoint;
    }

    public async override Task StopAsync(CancellationToken cancellationToken = default)
    {
        foreach (AzureServiceBusReceiveEndpoint endpoint in _receiveEndpoints) await endpoint.StopAsync(cancellationToken).ConfigureAwait(false);

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public async override ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        await _client.DisposeAsync().ConfigureAwait(false);
    }
}
