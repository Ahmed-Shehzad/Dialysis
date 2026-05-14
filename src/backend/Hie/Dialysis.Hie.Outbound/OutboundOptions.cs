namespace Dialysis.Hie.Outbound;

public sealed class OutboundOptions
{
    /// <summary>Default partner id used when an event-to-partner routing strategy is not yet configured.</summary>
    public string DefaultPartnerId { get; set; } = "default";

    /// <summary>Maximum delivery attempts before the bundle is marked Failed.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Backoff in seconds between attempts; capped exponentially.</summary>
    public int BackoffSeconds { get; set; } = 30;

    /// <summary>Number of pending bundles claimed per dispatcher tick.</summary>
    public int DispatchBatchSize { get; set; } = 16;

    /// <summary>Tick interval for the dispatcher hosted service.</summary>
    public TimeSpan TickInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>When true, dispatcher writes a domain delivery event via the outbox; false skips publish (useful in tests).</summary>
    public bool EmitDeliveryEvents { get; set; } = true;
}
