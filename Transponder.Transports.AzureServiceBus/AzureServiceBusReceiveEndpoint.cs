using Azure.Messaging.ServiceBus;

using Polly;

using Transponder.Transports.Abstractions;
using Transponder.Transports.AzureServiceBus.Abstractions;

namespace Transponder.Transports.AzureServiceBus;

internal sealed class AzureServiceBusReceiveEndpoint : IReceiveEndpoint
{
    private readonly ServiceBusProcessor _processor;
    private readonly Func<IReceiveContext, Task> _handler;
    private readonly Uri _inputAddress;
    private readonly Uri _hostAddress;
    private readonly ReceiveEndpointFaultSettings? _faultSettings;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ServiceBusSender? _deadLetterSender;

    public AzureServiceBusReceiveEndpoint(
        ServiceBusClient client,
        IReceiveEndpointConfiguration configuration,
        IAzureServiceBusTopology topology,
        Uri hostAddress,
        ReceiveEndpointFaultSettings? faultSettings,
        ResiliencePipeline resiliencePipeline)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(hostAddress);
        ArgumentNullException.ThrowIfNull(resiliencePipeline);

        _inputAddress = configuration.InputAddress;
        _handler = configuration.Handler ?? throw new ArgumentNullException(nameof(configuration.Handler));
        _hostAddress = hostAddress;
        _faultSettings = faultSettings;
        _resiliencePipeline = resiliencePipeline;

        var entity = AzureServiceBusEntityAddress.Parse(_inputAddress, topology);
        _processor = entity.IsSubscription
            ? client.CreateProcessor(entity.EntityPath, entity.SubscriptionName!)
            : client.CreateProcessor(entity.EntityPath);

        if (faultSettings?.DeadLetterAddress is not null)
        {
            var deadLetterEntity = AzureServiceBusEntityAddress.Parse(faultSettings.DeadLetterAddress, topology);
            _deadLetterSender = client.CreateSender(deadLetterEntity.EntityPath);
        }

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    public Uri InputAddress => _inputAddress;

    public Task StartAsync(CancellationToken cancellationToken = default)
        => _processor.StartProcessingAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default)
        => _processor.StopProcessingAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _processor.DisposeAsync().ConfigureAwait(false);

        if (_deadLetterSender is not null) await _deadLetterSender.DisposeAsync().ConfigureAwait(false);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        ServiceBusReceivedMessage? message = args.Message;
        var headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, object> header in message.ApplicationProperties) headers[header.Key] = header.Value;

        string? messageType = headers.TryGetValue("MessageType", out object? mt) ? mt as string : null;
        _ = headers.Remove("MessageType");
        Ulid? conversationId = headers.TryGetValue("ConversationId", out object? conv)
                               && Ulid.TryParse(conv?.ToString(), out Ulid parsedConversationId)
            ? parsedConversationId
            : (Ulid?)null;
        _ = headers.Remove("ConversationId");

        var transportMessage = new TransportMessage(
            message.Body.ToMemory(),
            message.ContentType,
            headers,
            new MessageIdentity(
                Ulid.TryParse(message.MessageId, out Ulid messageId) ? messageId : null,
                Ulid.TryParse(message.CorrelationId, out Ulid correlationId) ? correlationId : null,
                conversationId),
            messageType);

        var context = new AzureServiceBusReceiveContext(
            transportMessage,
            _hostAddress,
            _inputAddress,
            args.CancellationToken);

        try
        {
            await _resiliencePipeline.ExecuteAsync(
                    async ct => await _handler(context).ConfigureAwait(false),
                    args.CancellationToken)
                .ConfigureAwait(false);

            await args.CompleteMessageAsync(message, args.CancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (_deadLetterSender is not null)
            {
                ServiceBusMessage deadLetterMessage = CreateDeadLetterMessage(message);
                await _deadLetterSender.SendMessageAsync(deadLetterMessage, args.CancellationToken)
                    .ConfigureAwait(false);
                await args.CompleteMessageAsync(message, args.CancellationToken).ConfigureAwait(false);
                return;
            }

            if (_faultSettings?.UseTransportDeadLetter == true)
            {
                await args.DeadLetterMessageAsync(
                        message,
                        _faultSettings.DeadLetterReason,
                        _faultSettings.DeadLetterDescription,
                        args.CancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            await args.AbandonMessageAsync(message, cancellationToken: args.CancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static Task ProcessErrorAsync(ProcessErrorEventArgs args)
        => Task.CompletedTask;

    private static ServiceBusMessage CreateDeadLetterMessage(ServiceBusReceivedMessage message)
    {
        var deadLetterMessage = new ServiceBusMessage(message.Body)
        {
            ContentType = message.ContentType,
            CorrelationId = message.CorrelationId,
            MessageId = message.MessageId,
            Subject = message.Subject
        };

        foreach (KeyValuePair<string, object> header in message.ApplicationProperties) deadLetterMessage.ApplicationProperties[header.Key] = header.Value;

        return deadLetterMessage;
    }
}
