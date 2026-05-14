namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips.Events;

/// <summary>
/// Published when at least one <see cref="TransponderRoutingSlipActivityCompensationFailed"/> occurred during fault handling (after <see cref="TransponderRoutingSlipActivityFaulted"/>).
/// </summary>
public sealed class TransponderRoutingSlipCompensationFailed
{
    public required string TrackingNumber { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    public required string Reason { get; init; }

    public string? LastFailedActivityName { get; init; }

    public int? LastFailedActivityIndex { get; init; }
}
