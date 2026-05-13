using Dialysis.SmartConnect.Scheduling;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class TimeScheduleTests
{
    [Fact]
    public void Same_day_next_time()
    {
        var schedule = new TimeSchedule([new TimeOnly(9, 0), new TimeOnly(17, 0)]);
        var now = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

        var next = schedule.NextOccurrence(now);

        Assert.NotNull(next);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero), next!.Value);
    }

    [Fact]
    public void Between_two_times_picks_the_later_one()
    {
        var schedule = new TimeSchedule([new TimeOnly(9, 0), new TimeOnly(17, 0)]);
        var now = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        var next = schedule.NextOccurrence(now);

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 17, 0, 0, TimeSpan.Zero), next!.Value);
    }

    [Fact]
    public void After_last_time_rolls_to_next_day()
    {
        var schedule = new TimeSchedule([new TimeOnly(9, 0), new TimeOnly(17, 0)]);
        var now = new DateTimeOffset(2026, 1, 1, 18, 0, 0, TimeSpan.Zero);

        var next = schedule.NextOccurrence(now);

        Assert.Equal(new DateTimeOffset(2026, 1, 2, 9, 0, 0, TimeSpan.Zero), next!.Value);
    }

    [Fact]
    public void Empty_times_throws()
    {
        Assert.Throws<ArgumentException>(() => new TimeSchedule(Array.Empty<TimeOnly>()));
    }
}
