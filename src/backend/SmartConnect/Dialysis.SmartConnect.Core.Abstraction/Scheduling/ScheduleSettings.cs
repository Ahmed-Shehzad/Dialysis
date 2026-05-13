namespace Dialysis.SmartConnect.Scheduling;

/// <summary>
/// Mirth-equivalent polling schedule modes. Maps to the User Guide's source-connector polling settings
/// (Interval, Time, Cron) at pp 234-237 of v4.5.
/// </summary>
public enum ScheduleMode
{
    Interval = 1,
    Cron = 2,
    Time = 3,
}

/// <summary>
/// Serializable polling-schedule configuration for source connectors. Stored as JSON inside
/// <c>SourceConnectorContext.Parameters["schedule"]</c> and resolved into an <see cref="ISchedule"/>
/// by <c>ScheduleFactory</c> at connector start time.
/// </summary>
public sealed class ScheduleSettings
{
    public ScheduleMode Mode { get; set; } = ScheduleMode.Interval;

    /// <summary>Cron expression for <see cref="ScheduleMode.Cron"/>. 5-field (standard) or 6-field (with seconds).</summary>
    public string? CronExpression { get; set; }

    /// <summary>Polling interval in seconds for <see cref="ScheduleMode.Interval"/>.</summary>
    public int? IntervalSeconds { get; set; }

    /// <summary>Fixed wall-clock times for <see cref="ScheduleMode.Time"/>, in the configured timezone.</summary>
    public List<TimeOnly>? FixedTimes { get; set; }

    /// <summary>IANA / Windows time-zone id. Defaults to UTC if absent.</summary>
    public string? TimeZoneId { get; set; }

    /// <summary>Optional delay applied only before the very first occurrence (interval mode).</summary>
    public int? InitialDelaySeconds { get; set; }
}
