namespace Dialysis.SmartConnect.Scheduling;

/// <summary>
/// Fixed-times schedule (Mirth "Polling Type: Time"). Fires at one or more wall-clock times per day
/// in the configured timezone, rolling forward to the next day after the last time of the current day.
/// </summary>
public sealed class TimeSchedule : ISchedule
{
    private readonly IReadOnlyList<TimeOnly> _times;
    private readonly TimeZoneInfo _timeZone;

    public TimeSchedule(IEnumerable<TimeOnly> times, TimeZoneInfo? timeZone = null)
    {
        ArgumentNullException.ThrowIfNull(times);
        var list = times.Distinct().OrderBy(t => t).ToArray();
        if (list.Length == 0)
            throw new ArgumentException("TimeSchedule requires at least one fixed time.", nameof(times));
        _times = list;
        _timeZone = timeZone ?? TimeZoneInfo.Utc;
    }

    public DateTimeOffset? NextOccurrence(DateTimeOffset after)
    {
        var local = TimeZoneInfo.ConvertTime(after, _timeZone);
        var todayDate = DateOnly.FromDateTime(local.DateTime);
        var todayTime = TimeOnly.FromDateTime(local.DateTime);

        foreach (var t in _times)
        {
            if (t > todayTime)
                return ToOffset(todayDate, t);
        }

        var tomorrow = todayDate.AddDays(1);
        return ToOffset(tomorrow, _times[0]);
    }

    private DateTimeOffset ToOffset(DateOnly date, TimeOnly time)
    {
        var local = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, DateTimeKind.Unspecified);
        var offset = _timeZone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset);
    }
}
