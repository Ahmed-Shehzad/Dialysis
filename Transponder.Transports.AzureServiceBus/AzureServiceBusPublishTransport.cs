using Azure.Messaging.ServiceBus;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.AzureServiceBus;

internal sealed class AzureServiceBusPublishTransport : IPublishTransport
{
    private readonly ServiceBusSender _sender;

    public AzureServiceBusPublishTransport(ServiceBusClient client, string topicName)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (string.IsNullOrWhiteSpace(topicName)) throw new ArgumentException("Topic name must be provided.", nameof(topicName));

        _sender = client.CreateSender(topicName);
    }

    public async Task PublishAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var serviceBusMessage = new ServiceBusMessage(message.Body)
        {
            ContentType = message.ContentType,
            MessageId = message.MessageId?.ToString(),
            CorrelationId = message.CorrelationId?.ToString()
        };

        if (!string.IsNullOrWhiteSpace(message.MessageType)) serviceBusMessage.ApplicationProperties["MessageType"] = message.MessageType;

        if (message.ConversationId.HasValue) serviceBusMessage.ApplicationProperties["ConversationId"] = message.ConversationId.Value.ToString();

        foreach (KeyValuePair<string, object?> header in message.Headers)
        {
            if (header.Value is null) continue;

            serviceBusMessage.ApplicationProperties[header.Key] = header.Value;
        }

        await _sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
    }
}
