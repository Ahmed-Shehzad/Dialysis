using Amazon.SimpleNotificationService.Model;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Aws;

internal sealed class AwsPublishTransport : IPublishTransport
{
    private readonly AwsTransportHost _host;
    private readonly Type _messageType;

    public AwsPublishTransport(AwsTransportHost host, Type messageType)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _messageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
    }

    public async Task PublishAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        string topicArn = await _host.ResolveTopicArnAsync(_messageType, cancellationToken).ConfigureAwait(false);
        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = Convert.ToBase64String(message.Body.ToArray()),
            MessageAttributes = AwsTransportHost.BuildAttributes(message)
        };

        _ = await _host.SnsClient.PublishAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
