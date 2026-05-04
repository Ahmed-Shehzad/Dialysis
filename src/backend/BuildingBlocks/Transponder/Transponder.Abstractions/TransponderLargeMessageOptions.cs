namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>Limits and metadata for <see cref="ITransponderBus.PublishLargeAsync{TMessage}"/>.</summary>
public sealed class TransponderLargeMessageOptions
{
    /// <summary>Maximum size of each <see cref="TransponderMessageChunk.Segment"/> (serialized logical payload slice).</summary>
    public int MaxSegmentBytes { get; init; } = 256 * 1024;

    /// <summary>Upper bound on the serialized logical message size.</summary>
    public int MaxTotalPayloadBytes { get; init; } = 100 * 1024 * 1024;

    /// <summary>Maximum number of segments per session (safety cap).</summary>
    public int MaxChunkCount { get; init; } = 10_000;

    /// <summary>Optional correlation forwarded to each chunk publish.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>How long incomplete chunk sessions are retained before eviction.</summary>
    public TimeSpan IncompleteSessionTimeout { get; init; } = TimeSpan.FromMinutes(10);
}
