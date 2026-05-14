namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

/// <summary>Wire DTO for <see cref="TransponderSignalRHub.PublishAsync"/> and client <c>Receive</c>.</summary>
public sealed class TransponderSignalREnvelopeDto
{
    public required string RoutingKey { get; init; }

    public required byte[] Payload { get; init; }

    public string? CorrelationId { get; init; }

    public string? DeduplicationId { get; init; }

    public string? ContentType { get; init; }

    public Dictionary<string, string>? Headers { get; init; }
}
