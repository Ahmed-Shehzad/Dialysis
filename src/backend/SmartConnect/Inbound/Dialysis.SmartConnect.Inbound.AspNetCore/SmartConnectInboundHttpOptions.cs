namespace Dialysis.SmartConnect.Inbound.AspNetCore;

/// <summary>
/// Options for HTTP inbound endpoints (optional API key and payload limits).
/// Bind from configuration section <c>SmartConnect:InboundHttp</c>.
/// </summary>
public sealed class SmartConnectInboundHttpOptions
{
    /// <summary>
    /// When set, requests must include header <c>X-SmartConnect-ApiKey</c> with this exact value.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Maximum request body size in bytes (default 10 MiB).</summary>
    public long MaxRequestBodyBytes { get; set; } = 10 * 1024 * 1024;
}
