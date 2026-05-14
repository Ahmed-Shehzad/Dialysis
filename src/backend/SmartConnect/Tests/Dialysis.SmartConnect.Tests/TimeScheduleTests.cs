using Dialysis.SmartConnect.Scheduling;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class TimeScheduleTests
{
    [Fact]
    public void Same_Day_Next_Time()
    {
        var schedule = new TimeSchedule([new TimeOnly(9, 0), new TimeOnly(17, 0)]);
        var now = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

        var next = schedule.NextOccurrence(now);

        Assert.NotNull(next);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero), next!.Value);
    }

    [Fact]
    public void Between_Two_Times_Picks_The_Later_One()
    {
        var schedule = new TimeSchedule([new TimeOnly(9, 0), new TimeOnly(17, 0)]);
        var now = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        var next = schedule.NextOccurrence(now);

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 17, 0, 0, TimeSpan.Zero), next!.Value);
    }

    [Fact]
    public void After_Last_Time_Rolls_To_Next_Day()
    {
        var schedule = new TimeSchedule([new TimeOnly(9, 0), new TimeOnly(17, 0)]);
        var now = new DateTimeOffset(2026, 1, 1, 18, 0, 0, TimeSpan.Zero);

        var next = schedule.NextOccurrence(now);

        Assert.Equal(new DateTimeOffset(2026, 1, 2, 9, 0, 0, TimeSpan.Zero), next!.Value);
    }

    [Fact]
    public void Empty_Times_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TimeSchedule(Array.Empty<TimeOnly>()));
    }
}
