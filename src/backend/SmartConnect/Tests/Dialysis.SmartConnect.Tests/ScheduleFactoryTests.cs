using Dialysis.SmartConnect.Scheduling;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class ScheduleFactoryTests
{
    [Fact]
    public void Interval_Settings_Build_Interval_Schedule()
    {
        var schedule = ScheduleFactory.Build(new ScheduleSettings { Mode = ScheduleMode.Interval, IntervalSeconds = 30 });
        Assert.IsType<IntervalSchedule>(schedule);
    }

    [Fact]
    public void Cron_Settings_Build_Cron_Schedule()
    {
        var schedule = ScheduleFactory.Build(new ScheduleSettings { Mode = ScheduleMode.Cron, CronExpression = "* * * * *" });
        Assert.IsType<CronSchedule>(schedule);
    }

    [Fact]
    public void Time_Settings_Build_Time_Schedule()
    {
        var schedule = ScheduleFactory.Build(new ScheduleSettings
        {
            Mode = ScheduleMode.Time,
            FixedTimes = [new TimeOnly(9, 0)],
        });
        Assert.IsType<TimeSchedule>(schedule);
    }

    [Fact]
    public void Interval_Without_Seconds_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScheduleFactory.Build(new ScheduleSettings { Mode = ScheduleMode.Interval }));
    }

    [Fact]
    public void Cron_Without_Expression_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScheduleFactory.Build(new ScheduleSettings { Mode = ScheduleMode.Cron }));
    }

    [Fact]
    public void Time_Without_Fixed_Times_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScheduleFactory.Build(new ScheduleSettings { Mode = ScheduleMode.Time }));
    }

    [Fact]
    public void Unknown_Timezone_Throws()
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
    public void From_Parameters_Uses_Schedule_Key_When_Present()
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["schedule"] = """{"mode":"Cron","cronExpression":"* * * * *"}""",
        };

        var schedule = ScheduleFactory.FromParameters(parameters, fallbackIntervalSeconds: 5);

        Assert.IsType<CronSchedule>(schedule);
    }

    [Fact]
    public void From_Parameters_Falls_Back_To_Interval_When_No_Schedule_Key()
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var schedule = ScheduleFactory.FromParameters(parameters, fallbackIntervalSeconds: 7);

        Assert.IsType<IntervalSchedule>(schedule);
    }
}
