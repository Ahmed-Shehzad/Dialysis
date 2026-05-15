using System.Collections.Concurrent;
using System.Security.Cryptography;
using Dialysis.BuildingBlocks.Transponder.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Collects <see cref="TransponderMessageChunk"/> deliveries, verifies SHA-256, and dispatches the merged JSON to the logical routing key.
/// </summary>
public sealed class TransponderChunkReassemblyConsumer(
    TransponderConsumeDispatcher dispatcher,
    IMessageSerializer serializer,
    IOptions<TransponderLargeMessageOptions> largeOptions,
    ILogger<TransponderChunkReassemblyConsumer> logger) : IConsumer<TransponderMessageChunk>
{
    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();

    public async Task HandleAsync(ConsumeContext<TransponderMessageChunk> context)
    {
        var chunk = context.Message;
        var ttl = largeOptions.Value.IncompleteSessionTimeout;
        PruneStaleSessions(ttl);

        if (string.IsNullOrEmpty(chunk.LogicalRoutingKey)
            || chunk.TotalChunks < 1
            || chunk.ChunkIndex < 0
            || chunk.ChunkIndex >= chunk.TotalChunks
            || string.IsNullOrEmpty(chunk.PayloadSha256Hex)
            || chunk.Segment is null)
        {
            logger.LogWarning("Transponder chunk ignored: invalid metadata (session {Session})", chunk.ChunkSessionId);
            return;
        }

        var session = _sessions.GetOrAdd(chunk.ChunkSessionId, _ => new Session());
        byte[]? merged = null;
        try
        {
            lock (session.Sync)
            {
                session.Touch();

                if (session.LogicalRoutingKey is null)
                {
                    session.LogicalRoutingKey = chunk.LogicalRoutingKey;
                    session.PayloadSha256Hex = chunk.PayloadSha256Hex;
                    session.TotalChunks = chunk.TotalChunks;
                }
                else if (!string.Equals(session.LogicalRoutingKey, chunk.LogicalRoutingKey, StringComparison.Ordinal)
                    || !string.Equals(session.PayloadSha256Hex, chunk.PayloadSha256Hex, StringComparison.OrdinalIgnoreCase)
                    || session.TotalChunks != chunk.TotalChunks)
                {
                    logger.LogWarning(
                        "Transponder chunk session {Session} aborted: conflicting metadata",
                        chunk.ChunkSessionId);
                    _sessions.TryRemove(chunk.ChunkSessionId, out _);
                    return;
                }

                session.Segments[chunk.ChunkIndex] = chunk.Segment;
                if (session.Segments.Count != session.TotalChunks)
                    return;

                merged = session.Merge();
                if (!session.VerifyDigest(merged))
                {
                    logger.LogError(
                        "Transponder chunk session {Session} failed SHA-256 verification",
                        chunk.ChunkSessionId);
                    _sessions.TryRemove(chunk.ChunkSessionId, out _);
                    return;
                }

                _sessions.TryRemove(chunk.ChunkSessionId, out _);
            }
        }
        catch
        {
            _sessions.TryRemove(chunk.ChunkSessionId, out _);
            throw;
        }

        if (merged is null)
            return;

        var dedup = chunk.ChunkSessionId.ToString("N");
        await dispatcher
            .DispatchAsync(
                chunk.LogicalRoutingKey,
                merged,
                context.CorrelationId,
                dedup,
                serializer,
                context.Bus,
                context.CancellationToken)
            .ConfigureAwait(false);
    }

    private void PruneStaleSessions(TimeSpan ttl)
    {
        var cutoff = DateTimeOffset.UtcNow - ttl;
        foreach (var kv in _sessions)
        {
            if (kv.Value.LastSeenUtc < cutoff)
                _sessions.TryRemove(kv.Key, out _);
        }
    }

    private sealed class Session
    {
        public object Sync { get; } = new();
        public string? LogicalRoutingKey;
        public string? PayloadSha256Hex;
        public int TotalChunks;
        public DateTimeOffset LastSeenUtc = DateTimeOffset.UtcNow;
        public Dictionary<int, byte[]> Segments { get; } = new();

        public void Touch() => LastSeenUtc = DateTimeOffset.UtcNow;

        public byte[] Merge()
        {
            using var ms = new MemoryStream();
            for (var i = 0; i < TotalChunks; i++)
            {
                if (!Segments.TryGetValue(i, out var part))
                    throw new InvalidOperationException($"Missing chunk index {i}.");
                ms.Write(part, 0, part.Length);
            }

            return ms.ToArray();
        }

        public bool VerifyDigest(byte[] merged)
        {
            if (PayloadSha256Hex is null)
                return false;
            byte[] expected;
            try
            {
                expected = Convert.FromHexString(PayloadSha256Hex);
            }
            catch (FormatException)
            {
                return false;
            }

            var actual = SHA256.HashData(merged);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
    }
}
