using Transponder.Transports.Abstractions;

namespace Transponder.Transports.SSE;

internal sealed class SsePublishTransport : IPublishTransport
{
    private readonly SseClientRegistry _registry;

    public SsePublishTransport(
        SseClientRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public Task PublishAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SsePublishTargets targets = SsePublishTargetResolver.Resolve(message);
        return SsePublishDispatcher.PublishAsync(_registry, message, targets, cancellationToken);
    }
}
