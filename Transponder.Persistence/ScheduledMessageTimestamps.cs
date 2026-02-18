namespace Transponder.Persistence;

/// <summary>
/// Groups the timing-related fields for a scheduled message.
/// </summary>
public sealed record ScheduledMessageTimestamps(
    DateTimeOffset ScheduledTime,
    DateTimeOffset? CreatedTime = null,
    DateTimeOffset? DispatchedTime = null);
