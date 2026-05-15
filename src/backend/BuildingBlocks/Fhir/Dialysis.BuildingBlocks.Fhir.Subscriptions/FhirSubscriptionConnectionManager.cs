using System.Collections.Concurrent;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

/// <summary>
/// One live push connection (a WebSocket or an SSE response stream) bound to a subscription.
/// </summary>
public interface IFhirSubscriptionSink
{
    ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken);
}

/// <summary>
/// Tracks live WebSocket / SSE connections keyed by subscription id so the WebSocket and SSE
/// channel dispatchers push a notification only to the clients bound to that subscription —
/// unlike the Transponder bus transports, which fan out every envelope to every connection.
/// Connection lifetime mirrors <c>TransponderSseIngressRelay</c>: a concurrent registry plus a
/// per-sink write lock, with registration disposed when the request aborts.
/// </summary>
/// <remarks>
/// A bounded per-subscription replay buffer holds notifications that were pushed while no
/// connection was bound. When a client (re)connects, <see cref="FlushReplayAsync"/> drains the
/// buffer to it, so a WebSocket/SSE subscriber that briefly drops does not silently miss events.
/// The buffer is in-memory and bounded (oldest dropped past capacity); durable cross-restart
/// redelivery remains the job of the EF-Core-backed <c>NotificationOutbox</c> follow-up.
/// </remarks>
public sealed class FhirSubscriptionConnectionManager(int replayCapacity = 50)
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, IFhirSubscriptionSink>> _bindings =
        new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, ReplayBuffer> _replay = new(StringComparer.Ordinal);

    public IDisposable Register(string subscriptionId, IFhirSubscriptionSink sink)
    {
        ArgumentException.ThrowIfNullOrEmpty(subscriptionId);
        ArgumentNullException.ThrowIfNull(sink);

        var connections = _bindings.GetOrAdd(subscriptionId, static _ => new ConcurrentDictionary<Guid, IFhirSubscriptionSink>());
        var connectionId = Guid.NewGuid();
        connections[connectionId] = sink;
        return new Registration(this, subscriptionId, connectionId);
    }

    /// <summary>
    /// Drains the replay buffer for <paramref name="subscriptionId"/> into <paramref name="sink"/>.
    /// Called by the SSE/WebSocket endpoints immediately after binding a connection so the client
    /// receives any notifications that arrived while it was disconnected.
    /// </summary>
    public async ValueTask FlushReplayAsync(string subscriptionId, IFhirSubscriptionSink sink, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(subscriptionId);
        ArgumentNullException.ThrowIfNull(sink);

        if (!_replay.TryGetValue(subscriptionId, out var buffer))
            return;

        foreach (var payload in buffer.Drain())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await sink.SendAsync(payload, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Pushes <paramref name="payload"/> to every connection bound to the subscription. When no
    /// connection is bound the payload is buffered (bounded) for replay on the next reconnect.
    /// Returns the delivery count.
    /// </summary>
    public async ValueTask<int> PushAsync(string subscriptionId, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var delivered = 0;
        if (_bindings.TryGetValue(subscriptionId, out var connections))
        {
            foreach (var (connectionId, sink) in connections.ToArray())
            {
                try
                {
                    await sink.SendAsync(payload, cancellationToken).ConfigureAwait(false);
                    delivered++;
                }
                catch
                {
                    connections.TryRemove(connectionId, out _);
                }
            }
        }

        if (delivered == 0 && replayCapacity > 0)
        {
            var buffer = _replay.GetOrAdd(subscriptionId, _ => new ReplayBuffer(replayCapacity));
            buffer.Add(payload.ToArray());
        }

        return delivered;
    }

    private void Unregister(string subscriptionId, Guid connectionId)
    {
        if (_bindings.TryGetValue(subscriptionId, out var connections))
        {
            connections.TryRemove(connectionId, out _);
            if (connections.IsEmpty)
                _bindings.TryRemove(subscriptionId, out _);
        }
    }

    private sealed class Registration(FhirSubscriptionConnectionManager owner, string subscriptionId, Guid connectionId)
        : IDisposable
    {
        public void Dispose() => owner.Unregister(subscriptionId, connectionId);
    }

    private sealed class ReplayBuffer(int capacity)
    {
        private readonly Lock _gate = new();
        private readonly Queue<byte[]> _items = new();

        public void Add(byte[] payload)
        {
            lock (_gate)
            {
                _items.Enqueue(payload);
                while (_items.Count > capacity)
                    _items.Dequeue();
            }
        }

        public IReadOnlyList<byte[]> Drain()
        {
            lock (_gate)
            {
                if (_items.Count == 0)
                    return [];
                var snapshot = _items.ToArray();
                _items.Clear();
                return snapshot;
            }
        }
    }
}
