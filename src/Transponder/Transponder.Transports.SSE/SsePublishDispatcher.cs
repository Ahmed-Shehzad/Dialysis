using Transponder.Transports.Abstractions;

namespace Transponder.Transports.SSE;

internal static class SsePublishDispatcher
{
    public static Task PublishAsync(
        SseClientRegistry registry,
        ITransportMessage message,
        SsePublishTargets targets,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        var connections = registry.ResolveTargets(targets);
        if (connections.Count == 0) return Task.CompletedTask;

        var eventName = SsePublishTargetResolver.TryGetEventName(message, out var resolved)
            ? resolved
            : "transponder";

        var eventIdOverride = SsePublishTargetResolver.TryGetEventId(message);
        var envelope = SseTransportEnvelope.From(message);
        if (!string.IsNullOrWhiteSpace(eventIdOverride))
            envelope = envelope.WithId(eventIdOverride);

        var sseEvent = SseEvent.FromEnvelope(eventName, envelope);

        foreach (var connection in connections)
            _ = connection.TryEnqueue(sseEvent);

        return Task.CompletedTask;
    }
}
