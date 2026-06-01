namespace Dialysis.BuildingBlocks.ClinicianNotification;

/// <summary>
/// Cross-channel facade. Given a list of channel-targeted requests, the dispatcher tries each
/// sender registered for that channel in turn, stopping at the first delivery and reporting
/// the per-channel outcome to the caller (the on-call escalator uses the outcomes to advance
/// the escalation chain or terminate it as acknowledged).
/// </summary>
public interface IClinicianNotificationDispatcher
{
    Task<IReadOnlyList<ChannelOutcome>> DispatchAsync(
        IReadOnlyList<ClinicianNotificationRequest> requests,
        CancellationToken cancellationToken);
}

public sealed record ChannelOutcome(
    string Channel,
    string Address,
    ClinicianNotificationResult Result);
