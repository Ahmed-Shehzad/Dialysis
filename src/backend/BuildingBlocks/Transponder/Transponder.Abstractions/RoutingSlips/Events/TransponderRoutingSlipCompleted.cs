namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips.Events;

/// <summary>
/// Published when every itinerary activity has completed successfully and the slip row is removed.
/// </summary>
public sealed class TransponderRoutingSlipCompleted
{
    public required string TrackingNumber { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }
}
