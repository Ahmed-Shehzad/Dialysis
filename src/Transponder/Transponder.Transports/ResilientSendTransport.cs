using Polly;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

internal sealed class ResilientSendTransport : ISendTransport
{
    private readonly ISendTransport _inner;
    private readonly ResiliencePipeline _pipeline;

    public ResilientSendTransport(ISendTransport inner, ResiliencePipeline pipeline)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    public Task SendAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return _pipeline.ExecuteAsync(
            async ct => await _inner.SendAsync(message, ct).ConfigureAwait(false),
            cancellationToken).AsTask();
    }
}
