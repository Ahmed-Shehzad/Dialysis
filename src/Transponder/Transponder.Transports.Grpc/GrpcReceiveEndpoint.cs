using Polly;

using Transponder;
using Transponder.Transports;
using Transponder.Transports.Abstractions;

namespace Transponder.Transports.Grpc;

internal sealed class GrpcReceiveEndpoint : IReceiveEndpoint
{
    private readonly GrpcTransportHost _host;
    private readonly Func<IReceiveContext, Task> _handler;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly Uri? _deadLetterAddress;
    private readonly string? _deadLetterReason;
    private readonly string? _deadLetterDescription;

    public GrpcReceiveEndpoint(
        GrpcTransportHost host,
        IReceiveEndpointConfiguration configuration,
        ReceiveEndpointFaultSettings? faultSettings,
        ResiliencePipeline resiliencePipeline)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(resiliencePipeline);

        InputAddress = configuration.InputAddress;
        _handler = configuration.Handler ?? throw new ArgumentNullException(nameof(configuration.Handler));
        _resiliencePipeline = resiliencePipeline;
        _deadLetterAddress = faultSettings?.DeadLetterAddress;
        _deadLetterReason = faultSettings?.DeadLetterReason;
        _deadLetterDescription = faultSettings?.DeadLetterDescription;
    }

    public Uri InputAddress { get; }

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => new(StopAsync());

    internal Task HandleAsync(
        ITransportMessage message,
        Uri? sourceAddress,
        Uri? destinationAddress,
        CancellationToken cancellationToken)
    {
        var context = new GrpcReceiveContext(message, sourceAddress, destinationAddress, cancellationToken);
        return HandleInternalAsync(context, message, cancellationToken);
    }

    private async Task HandleInternalAsync(
        IReceiveContext context,
        ITransportMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _resiliencePipeline.ExecuteAsync(
                    async ct => await _handler(context).ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (_deadLetterAddress is null) throw;

            await SendToDeadLetterAsync(context, message, ex, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendToDeadLetterAsync(
        IReceiveContext context,
        ITransportMessage message,
        Exception ex,
        CancellationToken cancellationToken)
    {
        var sendTransport = await _host.GetSendTransportAsync(_deadLetterAddress!, cancellationToken)
            .ConfigureAwait(false);

        var headers = new Dictionary<string, object?>(message.Headers, StringComparer.OrdinalIgnoreCase)
        {
            ["DeadLetterReason"] = _deadLetterReason ?? "HandlerFailure",
            ["DeadLetterDescription"] = _deadLetterDescription ?? ex.Message,
            ["DeadLetterTime"] = DateTimeOffset.UtcNow.ToString("O")
        };

        if (context.DestinationAddress is not null &&
            !headers.ContainsKey(TransponderMessageHeaders.DestinationAddress))
            headers[TransponderMessageHeaders.DestinationAddress] = context.DestinationAddress.ToString();

        var deadLetterMessage = new TransportMessage(
            message.Body,
            message.ContentType,
            headers,
            message.MessageId,
            message.CorrelationId,
            message.ConversationId,
            message.MessageType,
            message.SentTime);

        await sendTransport.SendAsync(deadLetterMessage, cancellationToken).ConfigureAwait(false);
    }
}
