using System.Text.Json;

namespace Dialysis.SmartConnect.Scheduling;

/// <summary>Builds <see cref="ISchedule"/> instances from <see cref="ScheduleSettings"/>.</summary>
public static class ScheduleFactory
{
    private static readonly JsonSerializerOptions _scheduleJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    /// <summary>
    /// Resolves a schedule from a source-connector parameter dictionary. Reads <c>schedule</c> JSON when present;
    /// otherwise falls back to a fixed-interval schedule using <paramref name="fallbackIntervalSeconds"/>
    /// (typically the connector's pre-existing <c>PollIntervalSeconds</c> default).
    /// </summary>
    public static ISchedule FromParameters(IReadOnlyDictionary<string, string> parameters, int fallbackIntervalSeconds)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (fallbackIntervalSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(fallbackIntervalSeconds));

        var lookup = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);
        if (lookup.TryGetValue("schedule", out var json) && !string.IsNullOrWhiteSpace(json))
        {
            var settings = JsonSerializer.Deserialize<ScheduleSettings>(json, _scheduleJsonOptions)
                ?? throw new ArgumentException("schedule parameter was empty or invalid JSON.", nameof(parameters));
            return Build(settings);
        }

        return new IntervalSchedule(TimeSpan.FromSeconds(fallbackIntervalSeconds));
    }

    public static ISchedule Build(ScheduleSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var tz = ResolveTimeZone(settings.TimeZoneId);
        return settings.Mode switch
        {
            ScheduleMode.Interval => BuildInterval(settings),
            ScheduleMode.Cron => BuildCron(settings, tz),
            ScheduleMode.Time => BuildTime(settings, tz),
            _ => throw new ArgumentException($"Unknown schedule mode: {settings.Mode}.", nameof(settings)),
        };
    }

    private static IntervalSchedule BuildInterval(ScheduleSettings s)
    {
        var intervalSeconds = s.IntervalSeconds ?? 0;
        if (intervalSeconds <= 0)
            throw new ArgumentException("Interval schedule requires IntervalSeconds > 0.", nameof(s));
        TimeSpan? initial = s.InitialDelaySeconds is int delay && delay > 0
            ? TimeSpan.FromSeconds(delay)
            : null;
        return new IntervalSchedule(TimeSpan.FromSeconds(intervalSeconds), initial);
    }

    private static CronSchedule BuildCron(ScheduleSettings s, TimeZoneInfo tz)
    {
        if (string.IsNullOrWhiteSpace(s.CronExpression))
            throw new ArgumentException("Cron schedule requires CronExpression.", nameof(s));
        return new CronSchedule(s.CronExpression, tz);
    }

    private static TimeSchedule BuildTime(ScheduleSettings s, TimeZoneInfo tz)
    {
        if (s.FixedTimes is null || s.FixedTimes.Count == 0)
            throw new ArgumentException("Time schedule requires at least one FixedTimes entry.", nameof(s));
        return new TimeSchedule(s.FixedTimes, tz);
    }

    private static TimeZoneInfo ResolveTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return TimeZoneInfo.Utc;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new ArgumentException($"Unknown time-zone id '{id}'.", nameof(id), ex);
        }
    }
}
