namespace Dialysis.SmartConnect.TimeSync;

/// <summary>
/// Thread-safe register of inbound clock-skew observations. The inbound HL7v2 pipeline
/// calls <see cref="Record"/> on every parsed message; operator surfaces read via
/// <see cref="List"/> to render per-source health chips on the SmartConnect dashboard.
/// </summary>
public interface IClockSkewMonitor
{
    /// <summary>
    /// Record an observation. Implementations track a rolling absolute-skew envelope per
    /// source for the operator dashboard's "max drift in last N" indicator.
    /// </summary>
    void Record(ClockSkewObservation observation);

    /// <summary>Current per-source status snapshot, sorted by sourceId.</summary>
    IReadOnlyList<ClockSkewStatus> List();
}
