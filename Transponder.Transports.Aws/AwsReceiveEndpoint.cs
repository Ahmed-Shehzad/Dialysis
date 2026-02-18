using System.Text;

using Amazon.SQS.Model;

using Polly;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Aws;

internal sealed class AwsReceiveEndpoint : IReceiveEndpoint
{
    private readonly AwsTransportHost _host;
    private readonly Func<IReceiveContext, Task> _handler;
    private readonly Uri _inputAddress;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly AwsSendTransport? _deadLetterTransport;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public AwsReceiveEndpoint(
        AwsTransportHost host,
        IReceiveEndpointConfiguration configuration,
        ReceiveEndpointFaultSettings? faultSettings,
        ResiliencePipeline resiliencePipeline)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(resiliencePipeline);

        _inputAddress = configuration.InputAddress;
        _handler = configuration.Handler ?? throw new ArgumentNullException(nameof(configuration));
        _resiliencePipeline = resiliencePipeline;
        _deadLetterTransport = faultSettings?.DeadLetterAddress is null
            ? null
            : new AwsSendTransport(_host, faultSettings.DeadLetterAddress);
    }

    public Uri InputAddress => _inputAddress;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_loop is not null) return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loop = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is null) return;

        await _cts.CancelAsync();

        if (_loop is not null) await _loop.ConfigureAwait(false);

        _cts.Dispose();
        _cts = null;
        _loop = null;
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        string queueUrl = await _host.ResolveQueueUrlAsync(_inputAddress, cancellationToken).ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested)
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 10,
                MessageAttributeNames = ["All"]
            };

            ReceiveMessageResponse? response = await _host.SqsClient.ReceiveMessageAsync(request, cancellationToken)
                .ConfigureAwait(false);

            foreach (Message? message in response.Messages)
                await ProcessMessageAsync(queueUrl, message, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessMessageAsync(
        string queueUrl,
        Message message,
        CancellationToken cancellationToken)
    {
        ITransportMessage transportMessage = CreateTransportMessage(message);
        var context = new AwsReceiveContext(
            transportMessage,
            _host.Address,
            _inputAddress,
            cancellationToken);

        try
        {
            await _resiliencePipeline.ExecuteAsync(
                    async ct => await _handler(context).ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);
            _ = await _host.SqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            if (_deadLetterTransport is null) return;

            await TrySendToDeadLetterAndDeleteAsync(queueUrl, message, transportMessage, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task TrySendToDeadLetterAndDeleteAsync(
        string queueUrl,
        Message message,
        ITransportMessage transportMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _deadLetterTransport!.SendAsync(transportMessage, cancellationToken).ConfigureAwait(false);
            _ = await _host.SqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // Leave the message on the queue for retry if DLQ dispatch fails.
        }
    }

    private static ITransportMessage CreateTransportMessage(Message message)
    {
        var headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, MessageAttributeValue> attribute in message.MessageAttributes) headers[attribute.Key] = attribute.Value.StringValue;

        string? contentType = headers.TryGetValue("ContentType", out object? ct) ? ct as string : null;
        _ = headers.Remove("ContentType");

        string? messageType = headers.TryGetValue("MessageType", out object? mt) ? mt as string : null;
        _ = headers.Remove("MessageType");

        Ulid? correlationId = null;
        if (headers.TryGetValue("CorrelationId", out object? corr) &&
            Ulid.TryParse(corr as string, out Ulid corrId)) correlationId = corrId;

        _ = headers.Remove("CorrelationId");

        Ulid? conversationId = null;
        if (headers.TryGetValue("ConversationId", out object? conv) &&
            Ulid.TryParse(conv as string, out Ulid convId)) conversationId = convId;

        _ = headers.Remove("ConversationId");

        ReadOnlyMemory<byte> bodyBytes = DecodeBody(message.Body);

        return new TransportMessage(
            bodyBytes,
            contentType,
            headers,
            new MessageIdentity(
                Ulid.TryParse(message.MessageId, out Ulid parsedMessageId) ? parsedMessageId : null,
                correlationId,
                conversationId),
            messageType);
    }

    private static ReadOnlyMemory<byte> DecodeBody(string body)
    {
        try
        {
            return Convert.FromBase64String(body);
        }
        catch (FormatException)
        {
            return Encoding.UTF8.GetBytes(body);
        }
    }
}
