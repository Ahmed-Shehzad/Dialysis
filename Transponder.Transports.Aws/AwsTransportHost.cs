using System.Collections.Concurrent;

using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

using Polly;

using Transponder.Transports.Abstractions;
using Transponder.Transports.Aws.Abstractions;

namespace Transponder.Transports.Aws;

/// <summary>
/// AWS transport host backed by SQS/SNS.
/// </summary>
public sealed class AwsTransportHost : TransportHostBase
{
    private readonly AmazonSQSClient _sqsClient;
    private readonly AmazonSimpleNotificationServiceClient _snsClient;
    private readonly ConcurrentDictionary<string, string> _queueUrls = new();
    private readonly ConcurrentDictionary<string, string> _topicArns = new();
    private readonly List<AwsReceiveEndpoint> _receiveEndpoints = [];
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly TransportResilienceOptions? _resilienceOptions;

    public AwsTransportHost(IAwsTransportHostSettings settings)
        : base(settings?.Address ?? throw new ArgumentNullException(nameof(settings)))
    {
        Settings = settings;
        _resilienceOptions = (settings as ITransportHostResilienceSettings)?.ResilienceOptions;
        _resiliencePipeline = TransportResiliencePipeline.Create(_resilienceOptions);
        AWSCredentials? credentials = CreateCredentials(settings);
        var region = RegionEndpoint.GetBySystemName(settings.Region);

        var sqsConfig = new AmazonSQSConfig
        {
            RegionEndpoint = region,
            UseHttp = !settings.UseTls
        };

        if (!string.IsNullOrWhiteSpace(settings.ServiceUrl)) sqsConfig.ServiceURL = settings.ServiceUrl;

        _sqsClient = credentials is null
            ? new AmazonSQSClient(sqsConfig)
            : new AmazonSQSClient(credentials, sqsConfig);

        var snsConfig = new AmazonSimpleNotificationServiceConfig
        {
            RegionEndpoint = region,
            UseHttp = !settings.UseTls
        };

        if (!string.IsNullOrWhiteSpace(settings.ServiceUrl)) snsConfig.ServiceURL = settings.ServiceUrl;

        _snsClient = credentials is null
            ? new AmazonSimpleNotificationServiceClient(snsConfig)
            : new AmazonSimpleNotificationServiceClient(credentials, snsConfig);
    }

    public IAwsTransportHostSettings Settings { get; }

    public override Task<ISendTransport> GetSendTransportAsync(
        Uri address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        var transport = new AwsSendTransport(this, address);
        ISendTransport resilientTransport = TransportResiliencePipeline.WrapSend(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override Task<IPublishTransport> GetPublishTransportAsync(
        Type messageType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        var transport = new AwsPublishTransport(this, messageType);
        IPublishTransport resilientTransport = TransportResiliencePipeline.WrapPublish(transport, _resiliencePipeline);
        return Task.FromResult(resilientTransport);
    }

    public override IReceiveEndpoint ConnectReceiveEndpoint(IReceiveEndpointConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ReceiveEndpointFaultSettings? faultSettings = ReceiveEndpointFaultSettingsResolver.Resolve(configuration);
        ResiliencePipeline pipeline = TransportResiliencePipeline.Create(faultSettings?.ResilienceOptions ?? _resilienceOptions);
        var endpoint = new AwsReceiveEndpoint(this, configuration, faultSettings, pipeline);
        _receiveEndpoints.Add(endpoint);
        return endpoint;
    }

    public async override Task StopAsync(CancellationToken cancellationToken = default)
    {
        foreach (AwsReceiveEndpoint endpoint in _receiveEndpoints) await endpoint.StopAsync(cancellationToken).ConfigureAwait(false);

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private const string StringMessageAttributeDataType = "String";

    internal AmazonSQSClient SqsClient => _sqsClient;

    internal AmazonSimpleNotificationServiceClient SnsClient => _snsClient;

    internal async Task<string> ResolveQueueUrlAsync(Uri address, CancellationToken cancellationToken)
    {
        if (address.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)) return address.ToString();

        string queueName = Settings.Topology.GetQueueName(address);

        if (_queueUrls.TryGetValue(queueName, out string? cachedUrl)) return cachedUrl;

        GetQueueUrlResponse? response = await _sqsClient.GetQueueUrlAsync(queueName, cancellationToken).ConfigureAwait(false);
        _queueUrls[queueName] = response.QueueUrl;
        return response.QueueUrl;
    }

    internal async Task<string> ResolveTopicArnAsync(Type messageType, CancellationToken cancellationToken)
    {
        string topicName = Settings.Topology.GetTopicName(messageType);

        if (_topicArns.TryGetValue(topicName, out string? cachedArn)) return cachedArn;

        CreateTopicResponse? response = await _snsClient.CreateTopicAsync(topicName, cancellationToken).ConfigureAwait(false);
        _topicArns[topicName] = response.TopicArn;
        return response.TopicArn;
    }

    internal static Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>
        BuildAttributes(ITransportMessage message)
    {
        var attributes = new Dictionary<string, Amazon.SimpleNotificationService.Model.MessageAttributeValue>(
            StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(message.ContentType))
            attributes["ContentType"] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue
            {
                DataType = StringMessageAttributeDataType,
                StringValue = message.ContentType
            };

        if (!string.IsNullOrWhiteSpace(message.MessageType))
            attributes["MessageType"] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue
            {
                DataType = StringMessageAttributeDataType,
                StringValue = message.MessageType
            };

        if (message.MessageId.HasValue)
            attributes["MessageId"] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue
            {
                DataType = StringMessageAttributeDataType,
                StringValue = message.MessageId.Value.ToString()
            };

        if (message.CorrelationId.HasValue)
            attributes["CorrelationId"] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue
            {
                DataType = StringMessageAttributeDataType,
                StringValue = message.CorrelationId.Value.ToString()
            };

        if (message.ConversationId.HasValue)
            attributes["ConversationId"] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue
            {
                DataType = StringMessageAttributeDataType,
                StringValue = message.ConversationId.Value.ToString()
            };

        foreach (KeyValuePair<string, object?> header in message.Headers)
        {
            if (header.Value is null) continue;

            attributes[header.Key] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue
            {
                DataType = StringMessageAttributeDataType,
                StringValue = header.Value.ToString()
            };
        }

        return attributes;
    }

    public async override ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _sqsClient.Dispose();
        _snsClient.Dispose();
    }

    private static AWSCredentials? CreateCredentials(IAwsTransportHostSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.AccessKeyId) &&
            !string.IsNullOrWhiteSpace(settings.SecretAccessKey))
            return !string.IsNullOrWhiteSpace(settings.SessionToken)
                ? new SessionAWSCredentials(
                    settings.AccessKeyId,
                    settings.SecretAccessKey,
                    settings.SessionToken)
                : new BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey);

        return null;
    }
}
