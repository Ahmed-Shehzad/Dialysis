using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

/// <summary>Fans out published envelopes to every active gRPC <c>Subscribe</c> stream.</summary>
public sealed class TransponderGrpcIngressHub(ILogger<TransponderGrpcIngressHub> logger)
{
    private readonly ConcurrentDictionary<Guid, ChannelWriter<TransportEnvelope>> _writers = new();

    /// <summary>Registers a subscriber; disposing unregisters and completes the writer.</summary>
    public IDisposable Register(ChannelWriter<TransportEnvelope> writer)
    {
        var id = Guid.NewGuid();
        if (!_writers.TryAdd(id, writer))
            throw new InvalidOperationException("Transponder gRPC hub: failed to register subscriber.");

        return new Registration(this, id, writer);
    }

    /// <summary>Delivers a copy of the envelope to each subscriber (best effort).</summary>
    public async Task BroadcastAsync(TransportEnvelope envelope, CancellationToken cancellationToken)
    {
        foreach (var kv in _writers.ToArray())
        {
            try
            {
                await kv.Value.WriteAsync(Clone(envelope), cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                _writers.TryRemove(kv.Key, out _);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Transponder gRPC hub: failed writing to subscriber {Subscriber}", kv.Key);
            }
        }
    }

    private void Remove(Guid id) => _writers.TryRemove(id, out _);

    private static TransportEnvelope Clone(TransportEnvelope source)
    {
        var clone = new TransportEnvelope
        {
            RoutingKey = source.RoutingKey,
            Payload = Google.Protobuf.ByteString.CopyFrom(source.Payload.Span),
            CorrelationId = source.CorrelationId,
            DeduplicationId = source.DeduplicationId,
            ContentType = source.ContentType,
        };
        foreach (var kv in source.Headers)
            clone.Headers[kv.Key] = kv.Value;

        return clone;
    }

    private sealed class Registration(TransponderGrpcIngressHub hub, Guid id, ChannelWriter<TransportEnvelope> writer) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            hub.Remove(id);
            writer.TryComplete();
        }
    }
}
