using Confluent.Kafka;

using Polly;

using Transponder.Transports.Abstractions;
using Transponder.Transports.Kafka.Abstractions;

namespace Transponder.Transports.Kafka;

/// <summary>
/// Kafka transport host backed by Kafka client.
/// </summary>
public sealed class KafkaTransportHost : TransportHostBase
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly List<KafkaReceiveEndpoint> _receiveEndpoints = [];
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly TransportResilienceOptions? _resilienceOptions;

    public KafkaTransportHost(IKafkaHostSettings settings)
        : base(settings?.Address ?? throw new ArgumentNullException(nameof(settings)))
    {
        Settings = settings;
        _resilienceOptions = (settings as ITransportHostResilienceSettings)?.ResilienceOptions;
        _resiliencePipeline = TransportResiliencePipeline.Create(_resilienceOptions);
        _producer = new ProducerBuilder<string, byte[]>(BuildProducerConfig(settings)).Build();
    }

    public IKafkaHostSettings Settings { get; }

    public override Task<ISendTransport> GetSendTransportAsync(
        Uri address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        string topicName = Settings.Topology.GetTopicName(address);
        var transport = new KafkaSendTransport(_producer, topicName);
        ISendTransport resilientTransport = TransportResiliencePipeline.WrapSend(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override Task<IPublishTransport> GetPublishTransportAsync(
        Type messageType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        string topicName = Settings.Topology.GetTopicName(messageType);
        var transport = new KafkaPublishTransport(_producer, topicName);
        IPublishTransport resilientTransport = TransportResiliencePipeline.WrapPublish(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override IReceiveEndpoint ConnectReceiveEndpoint(IReceiveEndpointConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ReceiveEndpointFaultSettings? faultSettings = ReceiveEndpointFaultSettingsResolver.Resolve(configuration);
        ResiliencePipeline pipeline = TransportResiliencePipeline.Create(faultSettings?.ResilienceOptions ?? _resilienceOptions);
        var endpoint = new KafkaReceiveEndpoint(
            configuration,
            Settings,
            Address,
            _producer,
            faultSettings,
            pipeline);
        _receiveEndpoints.Add(endpoint);
        return endpoint;
    }

    public async override Task StopAsync(CancellationToken cancellationToken = default)
    {
        foreach (KafkaReceiveEndpoint endpoint in _receiveEndpoints) await endpoint.StopAsync(cancellationToken).ConfigureAwait(false);

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public async override ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _ = _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }

    private static ProducerConfig BuildProducerConfig(IKafkaHostSettings settings)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = string.Join(",", settings.BootstrapServers),
            ClientId = settings.ClientId
        };

        ApplySecuritySettings(config, settings);
        ApplyCustomSettings(config, settings.Settings);
        return config;
    }

    internal static ConsumerConfig BuildConsumerConfig(IKafkaHostSettings settings, string groupId)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = string.Join(",", settings.BootstrapServers),
            ClientId = settings.ClientId,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        ApplySecuritySettings(config, settings);
        ApplyCustomSettings(config, settings.Settings);
        return config;
    }

    private static void ApplySecuritySettings(ClientConfig config, IKafkaHostSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.SecurityProtocol) &&
            Enum.TryParse(settings.SecurityProtocol, true, out SecurityProtocol securityProtocol)) config.SecurityProtocol = securityProtocol;

        if (!string.IsNullOrWhiteSpace(settings.SaslMechanism) &&
            Enum.TryParse(settings.SaslMechanism, true, out SaslMechanism saslMechanism)) config.SaslMechanism = saslMechanism;

        if (!string.IsNullOrWhiteSpace(settings.SaslUsername)) config.SaslUsername = settings.SaslUsername;

        if (!string.IsNullOrWhiteSpace(settings.SaslPassword)) config.SaslPassword = settings.SaslPassword;
    }

    private static void ApplyCustomSettings(ClientConfig config, IReadOnlyDictionary<string, object?> settings)
    {
        foreach (KeyValuePair<string, object?> entry in settings)
        {
            if (entry.Value is null) continue;

            config.Set(entry.Key, entry.Value.ToString());
        }
    }
}
