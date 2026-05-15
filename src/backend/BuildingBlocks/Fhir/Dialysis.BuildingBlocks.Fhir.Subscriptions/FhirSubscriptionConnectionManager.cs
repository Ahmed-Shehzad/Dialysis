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
public sealed class FhirSubscriptionConnectionManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, IFhirSubscriptionSink>> _bindings =
        new(StringComparer.Ordinal);

    public IDisposable Register(string subscriptionId, IFhirSubscriptionSink sink)
    {
        ArgumentException.ThrowIfNullOrEmpty(subscriptionId);
        ArgumentNullException.ThrowIfNull(sink);

        var connections = _bindings.GetOrAdd(subscriptionId, static _ => new ConcurrentDictionary<Guid, IFhirSubscriptionSink>());
        var connectionId = Guid.NewGuid();
        connections[connectionId] = sink;
        return new Registration(this, subscriptionId, connectionId);
    }

    /// <summary>Pushes <paramref name="payload"/> to every connection bound to the subscription. Returns the delivery count.</summary>
    public async ValueTask<int> PushAsync(string subscriptionId, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (!_bindings.TryGetValue(subscriptionId, out var connections))
            return 0;

        var delivered = 0;
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
}
