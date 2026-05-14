using Dialysis.SmartConnect.Scheduling;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class IntervalScheduleTests
{
    [Fact]
    public void Next_Occurrence_Advances_By_Interval_From_Anchor()
    {
        var anchor = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var schedule = new IntervalSchedule(TimeSpan.FromSeconds(10), referenceStart: anchor);

        Assert.Equal(anchor + TimeSpan.FromSeconds(10), schedule.NextOccurrence(anchor + TimeSpan.FromSeconds(5)));
        Assert.Equal(anchor + TimeSpan.FromSeconds(20), schedule.NextOccurrence(anchor + TimeSpan.FromSeconds(15)));
        Assert.Equal(anchor + TimeSpan.FromSeconds(20), schedule.NextOccurrence(anchor + TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void Initial_Delay_Offsets_The_First_Fire()
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
    public void Negative_Interval_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IntervalSchedule(TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IntervalSchedule(TimeSpan.FromSeconds(-1)));
    }
}
