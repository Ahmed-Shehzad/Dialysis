namespace Dialysis.HIS.Integration.DeviceRegistry;

/// <summary>
/// Coarse edge abuse-guard for the device-reading ingest endpoint, bound from
/// <c>His:DeviceRegistry:Ingest:RateLimit</c>. This limiter only sheds gross floods at the HTTP
/// edge — it is NOT a per-device cap. Per-device fairness is enforced in-process by
/// <see cref="DeviceIngestion.SlidingWindowRateLimiter"/>, keyed on the body's <c>DeviceId</c>
/// (the device id is not available at the rate-limiter stage, which runs before model binding).
/// <para>
/// The default is sized for a real fleet (~200 req/s aggregate) because every device reading
/// reaches the HIS API through the per-context BFF + gateway, so the connection sees a single
/// upstream host — a per-device-sized cap here would throttle the whole fleet in aggregate.
/// </para>
/// </summary>
public sealed class DeviceIngestRateLimitOptions
{
    /// <summary>Configuration section these options bind from.</summary>
    public const string SectionName = "His:DeviceRegistry:Ingest:RateLimit";

    /// <summary>When false, the edge limiter is bypassed entirely (per-device governance still applies).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Max requests admitted per <see cref="WindowSeconds"/> per partition. Default ~200/s.</summary>
    public int PermitLimit { get; set; } = 12000;

    /// <summary>Fixed-window length in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Queued requests once the permit limit is reached. Default 0 — shed (429) rather than queue.</summary>
    public int QueueLimit { get; set; }
}
