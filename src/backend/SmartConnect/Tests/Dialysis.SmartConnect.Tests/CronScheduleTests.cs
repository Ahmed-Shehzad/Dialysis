using Dialysis.SmartConnect.Scheduling;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class CronScheduleTests
{
    [Fact]
    public void Five_field_cron_fires_at_every_minute()
    {
        var schedule = new CronSchedule("* * * * *");
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var next = schedule.NextOccurrence(now);

        Assert.NotNull(next);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 12, 1, 0, TimeSpan.Zero), next!.Value);
    }

    [Fact]
    public void Six_field_cron_fires_at_every_ten_seconds()
    {
        var schedule = new CronSchedule("0/10 * * * * *");
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var next = schedule.NextOccurrence(now);

        Assert.NotNull(next);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 12, 0, 10, TimeSpan.Zero), next!.Value);
    }

    [Fact]
    public void Specific_hour_in_named_timezone()
    {
        // Skip on platforms that don't have America/New_York in their tz database.
        TimeZoneInfo nyc;
        try
        {
            nyc = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        }
        catch (TimeZoneNotFoundException)
        {
            return;
        }

        var schedule = new CronSchedule("0 9 * * *", nyc);
        var utcNow = new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero); // midnight UTC = 8 pm EDT prev day
        var next = schedule.NextOccurrence(utcNow);

        Assert.NotNull(next);
        // 9 am New York = 13:00 UTC during EDT (Jun 15 is in DST).
        Assert.Equal(new DateTimeOffset(2026, 6, 15, 13, 0, 0, TimeSpan.Zero), next!.Value.ToUniversalTime());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("* *")]
    [InlineData("* * * *")]
    [InlineData("* * * * * * *")]
    public void Invalid_field_count_throws(string expression)
    {
        Assert.Throws<ArgumentException>(() => new CronSchedule(expression));
    }
}
