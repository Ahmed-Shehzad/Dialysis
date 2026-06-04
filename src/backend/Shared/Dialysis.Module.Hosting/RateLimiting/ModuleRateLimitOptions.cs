namespace Dialysis.Module.Hosting.RateLimiting;

/// <summary>
/// Per-module rate-limiting configuration. Bound from <c>&lt;Module&gt;:RateLimit</c> in
/// configuration; defaults are conservative enough to keep dev / smoke runs frictionless,
/// production deployments override per environment.
///
/// The middleware uses ASP.NET 7+ built-in token-bucket limiter, keyed on the authenticated
/// subject (<c>sub</c> claim) when present, falling back to <c>X-Forwarded-For</c> /
/// <c>RemoteIpAddress</c>. Each module API is rate-limited independently; the gateway
/// applies an outer cap on top.
/// </summary>
public sealed class ModuleRateLimitOptions
{
    public const string SectionName = "RateLimit";

    /// <summary>When <c>false</c> the middleware is not registered and no shedding happens.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Tokens replenished per second per partition (caller). With the default 200/sec a single
    /// authenticated clinician can sustain ~200 RPS — far more than realistic clinical use.
    /// Tune up for high-volume device feeds, down for tight per-tenant ceilings.
    /// </summary>
    public int TokensPerSecond { get; set; } = 200;

    /// <summary>
    /// Burst capacity — number of tokens a partition can accumulate before the bucket clamps.
    /// Should be ~1-3× <see cref="TokensPerSecond"/> so brief spikes pass without 429.
    /// </summary>
    public int BurstCapacity { get; set; } = 400;

    /// <summary>
    /// Maximum queued requests per partition when the bucket is empty. Beyond this, requests
    /// are rejected immediately with 429 instead of waiting. Keep small — long queues turn
    /// shedding into latency, which is worse for SLAs.
    /// </summary>
    public int QueueLimit { get; set; } = 0;
}
