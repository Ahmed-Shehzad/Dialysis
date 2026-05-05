namespace Dialysis.SmartConnect;

public sealed class DataPrunerOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(24);

    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);
}
