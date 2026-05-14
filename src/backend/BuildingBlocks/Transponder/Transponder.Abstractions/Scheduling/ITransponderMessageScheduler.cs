namespace Dialysis.BuildingBlocks.Transponder.Scheduling;

/// <summary>
/// Queues publication through <see cref="ITransponderBus"/> for a later instant (once) or on a recurring cron.
/// Register exactly one implementation per host (Hangfire or Quartz integration, etc.).
/// </summary>
public interface ITransponderMessageScheduler
{
    /// <summary>
    /// Schedules <see cref="ITransponderBus.PublishPreparedAsync"/> at <paramref name="runAt"/> (interpreted with <see cref="DateTimeOffset"/> clock semantics).
    /// </summary>
    /// <returns>An opaque id suitable for <see cref="TryCancelOnceAsync"/>.</returns>
    Task<string> ScheduleOnceAsync<TMessage>(
        TMessage message,
        DateTimeOffset runAt,
        TransponderPublishOptions? publishOptions = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Schedules the same publication on every cron tick. Cron dialect depends on the registered scheduler (Hangfire: NCrontab; Quartz: Quartz cron).
    /// </summary>
    /// <param name="scheduleId">Stable id for updates and <see cref="TryCancelRecurringAsync"/>; generated when null. TickerQ: when non-null, must be a Guid string.</param>
    /// <param name="timeZone">Calendar/time zone for cron evaluation; null uses UTC.</param>
    /// <returns>The recurring schedule id (same as <paramref name="scheduleId"/> when provided).</returns>
    Task<string> ScheduleRecurringAsync<TMessage>(
        TMessage message,
        string cronExpression,
        TransponderPublishOptions? publishOptions = null,
        string? scheduleId = null,
        TimeZoneInfo? timeZone = null,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Removes a one-time schedule if it exists and has not completed.</summary>
    Task<bool> TryCancelOnceAsync(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>Removes a recurring schedule if it exists.</summary>
    Task<bool> TryCancelRecurringAsync(string scheduleId, CancellationToken cancellationToken = default);
}
