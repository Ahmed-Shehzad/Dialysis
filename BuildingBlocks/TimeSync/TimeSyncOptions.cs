namespace BuildingBlocks.TimeSync;

/// <summary>
/// Configuration for HL7 message timestamp drift validation (IHE Consistent Time alignment).
/// When MaxAllowedDriftSeconds &gt; 0, HL7 ingest handlers log a warning if MSH-7 drifts from server UTC.
/// </summary>
public sealed class TimeSyncOptions
{
    public const string SectionName = "TimeSync";

    /// <summary>
    /// Maximum allowed drift in seconds between MSH-7 (message time) and server UTC.
    /// 0 = disabled. Default 300 (5 minutes).
    /// </summary>
    public int MaxAllowedDriftSeconds { get; set; } = 300;

    /// <summary>
    /// If true, log drift warnings at Warning level; else Debug.
    /// </summary>
    public bool LogDriftWarnings { get; set; } = true;
}
