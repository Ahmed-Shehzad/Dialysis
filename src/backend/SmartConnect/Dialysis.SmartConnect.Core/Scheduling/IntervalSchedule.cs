namespace Dialysis.SmartConnect.Scheduling;

/// <summary>
/// Fixed-interval schedule (Mirth "Polling Type: Interval"). The very first occurrence is offset by
/// <see cref="InitialDelay"/> from the schedule's reference start; subsequent occurrences are spaced
/// by <see cref="Interval"/>.
/// </summary>
public sealed class IntervalSchedule : ISchedule
{
    private readonly DateTimeOffset _start;

    public IntervalSchedule(TimeSpan interval, TimeSpan? initialDelay = null, DateTimeOffset? referenceStart = null)
    {
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive.");
        Interval = interval;
        InitialDelay = initialDelay ?? TimeSpan.Zero;
        _start = referenceStart ?? DateTimeOffset.MinValue;
    }

    public TimeSpan Interval { get; }

    public TimeSpan InitialDelay { get; }

    public DateTimeOffset? NextOccurrence(DateTimeOffset after)
    {
        var anchor = _start == DateTimeOffset.MinValue ? after : _start;
        var firstFire = anchor + InitialDelay;
        if (after < firstFire)
            return firstFire;

        var ticksSinceFirst = (after - firstFire).Ticks;
        var intervals = (ticksSinceFirst / Interval.Ticks) + 1;
        return firstFire + TimeSpan.FromTicks(intervals * Interval.Ticks);
    }
}
