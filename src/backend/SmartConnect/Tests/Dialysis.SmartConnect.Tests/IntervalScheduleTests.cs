using Dialysis.SmartConnect.Scheduling;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class IntervalScheduleTests
{
    [Fact]
    public void NextOccurrence_advances_by_interval_from_anchor()
    {
        var anchor = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var schedule = new IntervalSchedule(TimeSpan.FromSeconds(10), referenceStart: anchor);

        Assert.Equal(anchor + TimeSpan.FromSeconds(10), schedule.NextOccurrence(anchor + TimeSpan.FromSeconds(5)));
        Assert.Equal(anchor + TimeSpan.FromSeconds(20), schedule.NextOccurrence(anchor + TimeSpan.FromSeconds(15)));
        Assert.Equal(anchor + TimeSpan.FromSeconds(20), schedule.NextOccurrence(anchor + TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void Initial_delay_offsets_the_first_fire()
    {
        var anchor = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var schedule = new IntervalSchedule(
            TimeSpan.FromSeconds(10),
            initialDelay: TimeSpan.FromSeconds(60),
            referenceStart: anchor);

        Assert.Equal(anchor + TimeSpan.FromSeconds(60), schedule.NextOccurrence(anchor));
        Assert.Equal(anchor + TimeSpan.FromSeconds(70), schedule.NextOccurrence(anchor + TimeSpan.FromSeconds(60)));
    }

    [Fact]
    public void Negative_interval_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IntervalSchedule(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IntervalSchedule(TimeSpan.FromSeconds(-1)));
    }
}
