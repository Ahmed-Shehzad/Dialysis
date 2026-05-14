namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips.Events;

/// <summary>
/// Published when <see cref="IRoutingSlipCompensatableActivity.CompensateAsync"/> throws for one prior activity.
/// </summary>
public sealed class TransponderRoutingSlipActivityCompensationFailed
{
    public required string TrackingNumber { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    public required string ActivityName { get; init; }

    public required int ActivityIndex { get; init; }

    public required string Reason { get; init; }

    public string? ExceptionDetail { get; init; }
}
