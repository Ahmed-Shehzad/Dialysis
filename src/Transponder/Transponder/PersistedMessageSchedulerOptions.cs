namespace Transponder;

/// <summary>
/// Configures the persisted message scheduler.
/// </summary>
public sealed class PersistedMessageSchedulerOptions
{
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the dead-letter address for unresolvable messages.
    /// </summary>
    public Uri? DeadLetterAddress { get; set; }
}
