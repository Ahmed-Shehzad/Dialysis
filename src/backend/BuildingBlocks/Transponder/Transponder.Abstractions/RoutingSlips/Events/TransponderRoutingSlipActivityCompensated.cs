namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Published after <see cref="IRoutingSlipCompensatableActivity.CompensateAsync"/> completes successfully for one prior activity.
/// </summary>
public sealed class TransponderRoutingSlipActivityCompensated
{
    public required string TrackingNumber { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    public required string ActivityName { get; init; }

    public required int ActivityIndex { get; init; }
}
