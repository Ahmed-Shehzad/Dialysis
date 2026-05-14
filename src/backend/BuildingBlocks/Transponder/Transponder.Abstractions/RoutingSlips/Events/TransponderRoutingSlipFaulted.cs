namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips.Events;

/// <summary>
/// Published after a slip faults (activity failure, missing registration, or compensation phase). Observers should treat the slip as terminal.
/// </summary>
public sealed class TransponderRoutingSlipFaulted
{
    public required string TrackingNumber { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    public required string FaultReason { get; init; }

    public string? FaultExceptionDetail { get; init; }

    public string? FailedActivityName { get; init; }

    public int? FailedActivityIndex { get; init; }
}
