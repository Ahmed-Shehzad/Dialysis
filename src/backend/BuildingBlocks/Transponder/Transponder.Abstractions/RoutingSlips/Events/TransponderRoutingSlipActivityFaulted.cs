namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips.Events;

/// <summary>
/// Published when an activity <see cref="IRoutingSlipActivity.ExecuteAsync"/> throws or when an activity name cannot be resolved.
/// </summary>
public sealed class TransponderRoutingSlipActivityFaulted
{
    public required string TrackingNumber { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    public required string ActivityName { get; init; }

    public required int ActivityIndex { get; init; }

    public string? ArgumentsJson { get; init; }

    public required string FaultReason { get; init; }

    public string? FaultExceptionDetail { get; init; }
}
