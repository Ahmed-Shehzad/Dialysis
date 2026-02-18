using Transponder.Transports.Abstractions;

namespace Transponder.Transports.SSE;

internal sealed class SseSendTransport : ISendTransport
{
    private readonly Uri _destination;
    private readonly SseClientRegistry _registry;

    public SseSendTransport(
        Uri destination,
        SseClientRegistry registry)
    {
        _destination = destination ?? throw new ArgumentNullException(nameof(destination));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public Task SendAsync(ITransportMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SsePublishTargets targets = SsePublishTargetResolver.Resolve(
            message,
            _destination,
            allowDefaultBroadcast: false);

        if (!targets.Broadcast &&
            targets.ConnectionIds.Count == 0 &&
            targets.Streams.Count == 0 &&
            targets.Groups.Count == 0 &&
            targets.Users.Count == 0 &&
            targets.ExcludedConnectionIds.Count == 0)
            throw new InvalidOperationException(
                $"SSE send requires a target. Destination='{_destination}'.");

        return SsePublishDispatcher.PublishAsync(_registry, message, targets, cancellationToken);
    }
}
