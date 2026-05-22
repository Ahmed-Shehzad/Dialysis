namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Tuning knobs for <see cref="AttachmentOrphanReaperHostedService"/>. The reaper sweeps blobs whose
/// metadata insert never committed (blob-first ordering means the bytes land before the metadata row).
/// </summary>
public sealed record AttachmentOrphanReaperOptions
{
    /// <summary>How often the reaper wakes up to look for orphans. Default: 1 hour.</summary>
    public TimeSpan SweepInterval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Minimum age before a blob is eligible for reaping. Protects against the race where a blob
    /// has just been written but the metadata row hasn't committed yet. Default: 5 minutes.
    /// </summary>
    public TimeSpan GracePeriod { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Safety cap on how many blobs the reaper will delete in one sweep. Prevents a mass-delete
    /// if metadata was accidentally wiped. Default: 1000.
    /// </summary>
    public int MaxDeletionsPerSweep { get; init; } = 1000;
}
