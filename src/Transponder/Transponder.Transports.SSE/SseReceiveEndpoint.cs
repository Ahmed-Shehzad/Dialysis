using Polly;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports.SSE;

internal sealed class SseReceiveEndpoint : IReceiveEndpoint
{
    private readonly SseTransportHost _host;
    private readonly Func<IReceiveContext, Task> _handler;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly Uri? _deadLetterAddress;

    public SseReceiveEndpoint(
        SseTransportHost host,
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
        var context = new SseReceiveContext(message, sourceAddress, destinationAddress, cancellationToken);
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
        catch
        {
            if (_deadLetterAddress is null) throw;

            var sendTransport = await _host.GetSendTransportAsync(_deadLetterAddress, cancellationToken)
                .ConfigureAwait(false);
            await sendTransport.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}
