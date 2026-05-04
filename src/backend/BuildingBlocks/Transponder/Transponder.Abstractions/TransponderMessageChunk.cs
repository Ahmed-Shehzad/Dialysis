namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// One segment of a logical message published via <see cref="ITransponderBus.PublishLargeAsync{TMessage}"/>. The host reassembles segments, verifies the SHA-256 digest, then dispatches the merged payload to consumers of the logical contract.
/// </summary>
public sealed class TransponderMessageChunk
{
    /// <summary>Routing key of the original contract (<see cref="Type.FullName"/>).</summary>
    public required string LogicalRoutingKey { get; init; }

    /// <summary>Identifies all segments that belong to one logical publish.</summary>
    public required Guid ChunkSessionId { get; init; }

    /// <summary>Zero-based index, strictly less than <see cref="TotalChunks"/>.</summary>
    public required int ChunkIndex { get; init; }

    /// <summary>Total number of segments for this session.</summary>
    public required int TotalChunks { get; init; }

    /// <summary>Uppercase hex SHA-256 of the full merged payload (same on every segment).</summary>
    public required string PayloadSha256Hex { get; init; }

    /// <summary>Raw JSON bytes for this segment of the serialized logical message.</summary>
    public required byte[] Segment { get; init; }
}
