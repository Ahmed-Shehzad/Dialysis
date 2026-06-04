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
public sealed class FhirSubscriptionConnectionManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, IFhirSubscriptionSink>> _bindings =
        new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, ReplayBuffer> _replay = new(StringComparer.Ordinal);
    private readonly int _replayCapacity;
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
    public FhirSubscriptionConnectionManager(int replayCapacity = 50) => _replayCapacity = replayCapacity;

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

        if (delivered == 0 && _replayCapacity > 0)
        {
            var buffer = _replay.GetOrAdd(subscriptionId, _ => new ReplayBuffer(_replayCapacity));
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

    private sealed class Registration : IDisposable
    {
        private readonly FhirSubscriptionConnectionManager _owner;
        private readonly string _subscriptionId;
        private readonly Guid _connectionId;
        public Registration(FhirSubscriptionConnectionManager owner, string subscriptionId, Guid connectionId)
        {
            _owner = owner;
            _subscriptionId = subscriptionId;
            _connectionId = connectionId;
        }
        public void Dispose() => _owner.Unregister(_subscriptionId, _connectionId);
    }

    private sealed class ReplayBuffer
    {
        private readonly Lock _gate = new();
        private readonly Queue<byte[]> _items = new();
        private readonly int _capacity;
        public ReplayBuffer(int capacity) => _capacity = capacity;

        public void Add(byte[] payload)
        {
            lock (_gate)
            {
                _items.Enqueue(payload);
                while (_items.Count > _capacity)
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
