using Cronos;

namespace Dialysis.SmartConnect.Scheduling;

/// <summary>
/// Cron-expression-driven schedule (Mirth "Polling Type: Cron"). Wraps <c>Cronos.CronExpression</c>
/// (cref'd as plain text: Hangfire.Core embeds its own internal Cronos, which would make a resolved
/// cref ambiguous).
/// 5-field (standard, minute precision) and 6-field (with seconds) expressions are both supported;
/// the constructor detects field count automatically.
/// </summary>
public sealed class CronSchedule : ISchedule
{
    private readonly CronExpression _expression;
    private readonly TimeZoneInfo _timeZone;

    public CronSchedule(string expression, TimeZoneInfo? timeZone = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        _timeZone = timeZone ?? TimeZoneInfo.Utc;

        var trimmed = expression.Trim();
        var fieldCount = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        _expression = fieldCount switch
        {
            5 => CronExpression.Parse(trimmed),
            6 => CronExpression.Parse(trimmed, CronFormat.IncludeSeconds),
            _ => throw new ArgumentException(
                $"Cron expression must have 5 or 6 fields (got {fieldCount}).",
                nameof(expression)),
        };
    }

    public DateTimeOffset? NextOccurrence(DateTimeOffset after) =>
        _expression.GetNextOccurrence(after, _timeZone, inclusive: false);
}
