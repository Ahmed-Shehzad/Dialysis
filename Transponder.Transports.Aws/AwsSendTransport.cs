using Amazon.SQS.Model;

using Transponder.Transports.Abstractions;

using SnsMessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace Transponder.Transports.Aws;

internal sealed class AwsSendTransport : ISendTransport
{
    private readonly AwsTransportHost _host;
    private readonly Uri _address;

    public AwsSendTransport(AwsTransportHost host, Uri address)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _address = address ?? throw new ArgumentNullException(nameof(address));
    }

    public async Task SendAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        string queueUrl = await _host.ResolveQueueUrlAsync(_address, cancellationToken).ConfigureAwait(false);
        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = Convert.ToBase64String(message.Body.ToArray()),
            MessageAttributes = AwsTransportHost.BuildAttributes(message)
                .ToDictionary(kvp => kvp.Key, kvp => ToSqsAttribute(kvp.Value))
        };

        _ = await _host.SqsClient.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static MessageAttributeValue ToSqsAttribute(SnsMessageAttributeValue attribute)
        => new()
        {
            DataType = attribute.DataType,
            StringValue = attribute.StringValue
        };
}
