namespace Transponder;

/// <summary>
/// Configures durable outbox dispatch behavior.
/// </summary>
public sealed class OutboxDispatchOptions
{
    public int ChannelCapacity { get; set; } = 1024;

    public int BatchSize { get; set; } = 100;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    public int MaxConcurrentDestinations { get; set; } = Math.Max(1, Environment.ProcessorCount);

    /// <summary>
    /// Gets or sets the dead-letter address for unresolvable messages.
    /// </summary>
    public Uri? DeadLetterAddress { get; set; }
}
