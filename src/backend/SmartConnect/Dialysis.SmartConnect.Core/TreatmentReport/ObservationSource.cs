namespace Dialysis.SmartConnect.TreatmentReport;

/// <summary>
/// IG §8.2.5 OBX-17 Observation Method codes for dialysis-machine observations.
/// </summary>
/// <remarks>
/// The IG distinguishes two categories of observation source:
/// <list type="bullet">
///   <item><b>Measurements</b> — values the machine reads from sensors. Either
///         <see cref="AutoMeasurement"/> (arterial pressure, dialysate temp) or
///         <see cref="ManualMeasurement"/> (clinician-triggered, e.g. NIBP BP).
///         OBX-17 is optional per IG §8.2.5.</item>
///   <item><b>Settings</b> — control values. Provenance is tracked through three
///         states: <see cref="RemoteSetting"/> = received from EMR prescription
///         and unchanged; <see cref="ManualSetting"/> = clinician overrode on the
///         machine; <see cref="AutoSetting"/> = the machine's internal control
///         changed it. OBX-17 is REQUIRED for settings per IG §8.2.5. Per §5,
///         once a remote setting transitions to manual or automatic it cannot
///         return to remote even if the user restores the original value — the
///         provenance flip is one-way.</item>
/// </list>
/// </remarks>
public enum ObservationSource
{
    /// <summary>Sensor reading taken automatically by the machine.</summary>
    AutoMeasurement = 1,

    /// <summary>Sensor reading taken due to user interaction (e.g. NIBP cuff).</summary>
    ManualMeasurement = 2,

    /// <summary>Setting received from EMR prescription, never modified locally.</summary>
    RemoteSetting = 3,

    /// <summary>Setting changed by the user on the machine.</summary>
    ManualSetting = 4,

    /// <summary>Setting changed by the machine's internal control algorithm.</summary>
    AutoSetting = 5,
}

public static class ObservationSourceExtensions
{
    /// <summary>The wire token used in OBX-17 component 1 per IG §8.2.5.</summary>
    public static string ToObx17Token(this ObservationSource source) => source switch
    {
        ObservationSource.AutoMeasurement => "AMEAS",
        ObservationSource.ManualMeasurement => "MMEAS",
        ObservationSource.RemoteSetting => "RSET",
        ObservationSource.ManualSetting => "MSET",
        ObservationSource.AutoSetting => "ASET",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown observation source."),
    };

    /// <summary>Full OBX-17 CWE value: <c>TOKEN^human label^MDC</c>.</summary>
    public static string ToObx17Cwe(this ObservationSource source) => source switch
    {
        ObservationSource.AutoMeasurement => "AMEAS^auto-measurement^MDC",
        ObservationSource.ManualMeasurement => "MMEAS^manual-measurement^MDC",
        ObservationSource.RemoteSetting => "RSET^remote-setting^MDC",
        ObservationSource.ManualSetting => "MSET^manual-setting^MDC",
        ObservationSource.AutoSetting => "ASET^auto-setting^MDC",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown observation source."),
    };

    /// <summary><c>true</c> when the source represents a control value (setting), not a measurement.</summary>
    public static bool IsSetting(this ObservationSource source) =>
        source is ObservationSource.RemoteSetting
              or ObservationSource.ManualSetting
              or ObservationSource.AutoSetting;
}
