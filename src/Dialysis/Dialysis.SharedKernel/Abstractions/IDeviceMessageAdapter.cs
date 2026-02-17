namespace Dialysis.SharedKernel.Abstractions;

/// <summary>
/// Transforms vendor-specific device output into PDMS-standard format. Phase 1.1.4.
/// Implement per-vendor adapters for machine-specific protocols.
/// </summary>
public interface IDeviceMessageAdapter
{
    /// <summary>
    /// Adapter identifier (e.g. "fresenius-5008", "baxter-ak98").
    /// </summary>
    string AdapterId { get; }

    /// <summary>
    /// Returns true if this adapter can handle the raw message.
    /// </summary>
    bool CanHandle(string rawMessage);

    /// <summary>
    /// Transforms raw device output to standard JSON for vitals ingest.
    /// Returns null if transformation fails.
    /// </summary>
    /// <param name="rawMessage">Raw device output (proprietary format).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON matching IngestVitalsRequest schema, or null on failure.</returns>
    Task<string?> TransformToVitalsJsonAsync(string rawMessage, CancellationToken cancellationToken = default);
}
