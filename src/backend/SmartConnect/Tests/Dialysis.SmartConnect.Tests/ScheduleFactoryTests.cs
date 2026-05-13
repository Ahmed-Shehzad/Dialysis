using Dialysis.SmartConnect.Scheduling;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class ScheduleFactoryTests
{
    [Fact]
    public void Interval_settings_build_IntervalSchedule()
    {
        var schedule = ScheduleFactory.Build(new ScheduleSettings { Mode = ScheduleMode.Interval, IntervalSeconds = 30 });
        Assert.IsType<IntervalSchedule>(schedule);
    }

    [Fact]
    public void Cron_settings_build_CronSchedule()
    {
        var schedule = ScheduleFactory.Build(new ScheduleSettings { Mode = ScheduleMode.Cron, CronExpression = "* * * * *" });
        Assert.IsType<CronSchedule>(schedule);
    }

    [Fact]
    public void Time_settings_build_TimeSchedule()
    {
        var schedule = ScheduleFactory.Build(new ScheduleSettings
        {
            Mode = ScheduleMode.Time,
            FixedTimes = [new TimeOnly(9, 0)],
        });
        Assert.IsType<TimeSchedule>(schedule);
    }

    [Fact]
    public void Interval_without_seconds_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScheduleFactory.Build(new ScheduleSettings { Mode = ScheduleMode.Interval }));
    }

    [Fact]
    public void Cron_without_expression_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScheduleFactory.Build(new ScheduleSettings { Mode = ScheduleMode.Cron }));
    }

    [Fact]
    public void Time_without_fixed_times_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScheduleFactory.Build(new ScheduleSettings { Mode = ScheduleMode.Time }));
    }

    [Fact]
    public void Unknown_timezone_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScheduleFactory.Build(new ScheduleSettings
            {
                Mode = ScheduleMode.Cron,
                CronExpression = "* * * * *",
                TimeZoneId = "Mars/Olympus",
            }));
    }

    [Fact]
    public void FromParameters_uses_schedule_key_when_present()
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["schedule"] = """{"mode":"Cron","cronExpression":"* * * * *"}""",
        };

        var schedule = ScheduleFactory.FromParameters(parameters, fallbackIntervalSeconds: 5);

        Assert.IsType<CronSchedule>(schedule);
    }

    [Fact]
    public void FromParameters_falls_back_to_interval_when_no_schedule_key()
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var schedule = ScheduleFactory.FromParameters(parameters, fallbackIntervalSeconds: 7);

        Assert.IsType<IntervalSchedule>(schedule);
    }
}
