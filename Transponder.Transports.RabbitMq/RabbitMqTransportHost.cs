using Polly;

using RabbitMQ.Client;

using Transponder.Transports.Abstractions;
using Transponder.Transports.RabbitMq.Abstractions;

namespace Transponder.Transports.RabbitMq;

/// <summary>
/// RabbitMQ transport host backed by AMQP connections.
/// </summary>
public sealed class RabbitMqTransportHost : TransportHostBase
{
    private readonly ConnectionFactory _factory;
    private readonly Lock _connectionSync = new();
    private Task<IConnection>? _connectionTask;
    private readonly List<RabbitMqReceiveEndpoint> _receiveEndpoints = [];
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly TransportResilienceOptions? _resilienceOptions;

    public RabbitMqTransportHost(IRabbitMqHostSettings settings)
        : base(settings?.Address ?? throw new ArgumentNullException(nameof(settings)))
    {
        Settings = settings;
        _resilienceOptions = (settings as ITransportHostResilienceSettings)?.ResilienceOptions;
        _resiliencePipeline = TransportResiliencePipeline.Create(_resilienceOptions);
        _factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            VirtualHost = settings.VirtualHost,
            ConsumerDispatchConcurrency = 1
        };

        if (!string.IsNullOrWhiteSpace(settings.Username)) _factory.UserName = settings.Username;

        if (!string.IsNullOrWhiteSpace(settings.Password)) _factory.Password = settings.Password;

        if (settings.UseTls) _factory.Ssl.Enabled = true;

        if (settings.RequestedHeartbeat.HasValue) _factory.RequestedHeartbeat = settings.RequestedHeartbeat.Value;
    }

    public IRabbitMqHostSettings Settings { get; }

    public async override Task StartAsync(CancellationToken cancellationToken = default)
    {
        _ = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async override Task<ISendTransport> GetSendTransportAsync(
        Uri address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        IConnection connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        string queueName = Settings.Topology.GetQueueName(address);
        var transport = new RabbitMqSendTransport(connection, queueName);
        ISendTransport resilientTransport = TransportResiliencePipeline.WrapSend(transport, _resiliencePipeline);
        return resilientTransport;
    }

    public async override Task<IPublishTransport> GetPublishTransportAsync(
        Type messageType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        IConnection connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        string exchangeName = Settings.Topology.GetExchangeName(messageType);
        var transport = new RabbitMqPublishTransport(
            connection,
            exchangeName,
            Settings.Topology,
            messageType);
        IPublishTransport resilientTransport = TransportResiliencePipeline.WrapPublish(transport, _resiliencePipeline);
        return resilientTransport;
    }

    public override IReceiveEndpoint ConnectReceiveEndpoint(IReceiveEndpointConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ReceiveEndpointFaultSettings? faultSettings = ReceiveEndpointFaultSettingsResolver.Resolve(configuration);
        ResiliencePipeline pipeline = TransportResiliencePipeline.Create(faultSettings?.ResilienceOptions ?? _resilienceOptions);
        var endpoint = new RabbitMqReceiveEndpoint(
            GetConnectionAsync,
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
        foreach (RabbitMqReceiveEndpoint endpoint in _receiveEndpoints) await endpoint.StopAsync(cancellationToken).ConfigureAwait(false);

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public async override ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        if (_connectionTask is null) return;
        IConnection connection = await _connectionTask.ConfigureAwait(false);
        await connection.DisposeAsync();
    }

    private Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        lock (_connectionSync)
        {
            if (_connectionTask is null || _connectionTask.IsCanceled || _connectionTask.IsFaulted) _connectionTask = _factory.CreateConnectionAsync(cancellationToken);

            return _connectionTask;
        }
    }
}
