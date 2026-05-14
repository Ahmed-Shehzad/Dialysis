using System.Security.Cryptography;
using Dialysis.BuildingBlocks.Transponder.Serialization;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Splits a serialized message into <see cref="TransponderMessageChunk"/> frames and publishes them with a stable digest for integrity.
/// </summary>
public static class TransponderLargeMessagePublisher
{
    public static async Task PublishAsync<TMessage>(
        ITransponderBus bus,
        IMessageSerializer serializer,
        TMessage message,
        TransponderLargeMessageOptions? options,
        CancellationToken cancellationToken)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(message);

        var o = options ?? new TransponderLargeMessageOptions();
        if (o.MaxSegmentBytes < 1024)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxSegmentBytes must be at least 1024.");
        if (o.MaxTotalPayloadBytes < o.MaxSegmentBytes)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxTotalPayloadBytes must be at least MaxSegmentBytes.");

        var full = serializer.Serialize(message);
        if (full.Length > o.MaxTotalPayloadBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(message),
                $"Serialized payload length {full.Length} exceeds MaxTotalPayloadBytes ({o.MaxTotalPayloadBytes}).");
        }

        var correlation = o.CorrelationId ?? Guid.NewGuid().ToString("N");
        if (full.Length <= o.MaxSegmentBytes)
        {
            await bus.PublishAsync(message, new TransponderPublishOptions(correlation), cancellationToken).ConfigureAwait(false);
            return;
        }

        var shaHex = Convert.ToHexString(SHA256.HashData(full.Span));
        var session = Guid.NewGuid();
        var logicalKey = RoutingKey.For<TMessage>();
        var total = (int)Math.Ceiling(full.Length / (double)o.MaxSegmentBytes);
        if (total > o.MaxChunkCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(message),
                $"Chunk count {total} exceeds MaxChunkCount ({o.MaxChunkCount}).");
        }

        for (var i = 0; i < total; i++)
        {
            var offset = i * o.MaxSegmentBytes;
            var len = Math.Min(o.MaxSegmentBytes, full.Length - offset);
            var segment = full.Slice(offset, len).ToArray();
            var chunk = new TransponderMessageChunk
            {
                LogicalRoutingKey = logicalKey,
                ChunkSessionId = session,
                ChunkIndex = i,
                TotalChunks = total,
                PayloadSha256Hex = shaHex,
                Segment = segment,
            };
            var chunkDedup = $"{session:N}:{i}";
            await bus
                .PublishAsync(chunk, new TransponderPublishOptions(correlation, chunkDedup), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
