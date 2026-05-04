namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Published after an itinerary activity executes successfully (including the final activity before <see cref="TransponderRoutingSlipCompleted"/>).
/// </summary>
public sealed class TransponderRoutingSlipActivityCompleted
{
    public required string TrackingNumber { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    public required string ActivityName { get; init; }

    public required int ActivityIndex { get; init; }

    public string? ArgumentsJson { get; init; }
}
