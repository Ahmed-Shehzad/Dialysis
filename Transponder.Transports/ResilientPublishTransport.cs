using Polly;

using Transponder.Transports.Abstractions;

namespace Transponder.Transports;

internal sealed class ResilientPublishTransport : IPublishTransport
{
    private readonly IPublishTransport _inner;
    private readonly ResiliencePipeline _pipeline;

    public ResilientPublishTransport(IPublishTransport inner, ResiliencePipeline pipeline)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    public Task PublishAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return _pipeline.ExecuteAsync(
            async ct => await _inner.PublishAsync(message, ct).ConfigureAwait(false),
            cancellationToken).AsTask();
    }
}
