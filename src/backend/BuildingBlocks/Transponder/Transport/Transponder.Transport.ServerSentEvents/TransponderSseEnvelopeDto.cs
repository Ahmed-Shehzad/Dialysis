namespace Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents;

/// <summary>JSON body for POST publish and payload inside each SSE <c>data:</c> line.</summary>
public sealed class TransponderSseEnvelopeDto
{
    public required string RoutingKey { get; init; }

    public required byte[] Payload { get; init; }

    public string? CorrelationId { get; init; }

    public string? DeduplicationId { get; init; }

    public string? ContentType { get; init; }

    public Dictionary<string, string>? Headers { get; init; }
}
