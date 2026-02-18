using Azure.Messaging.ServiceBus;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.AzureServiceBus;

internal sealed class AzureServiceBusSendTransport : ISendTransport
{
    private readonly ServiceBusSender _sender;

    public AzureServiceBusSendTransport(ServiceBusClient client, string entityPath)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (string.IsNullOrWhiteSpace(entityPath)) throw new ArgumentException("Entity path must be provided.", nameof(entityPath));

        _sender = client.CreateSender(entityPath);
    }

    public async Task SendAsync(ITransportMessage message, CancellationToken cancellationToken = default)
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
