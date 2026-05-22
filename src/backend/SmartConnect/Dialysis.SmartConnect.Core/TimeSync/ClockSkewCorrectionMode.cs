namespace Dialysis.SmartConnect.TimeSync;

/// <summary>
/// Controls whether <see cref="Hl7V2ClockSkewProbe.TryObserveAndCorrect"/> just reports
/// drift (the historical behaviour from PR #53) or also rewrites <c>MSH-7</c> to the
/// server's UTC clock before downstream stages see the message.
/// </summary>
/// <remarks>
/// IG §2: every dialysis machine on a compliant deployment is expected to participate in
/// IHE Consistent Time (NTP / RFC 1305). When a machine drifts, treatment timestamps land
/// on the wrong side of midnight, dose-window correlations break, and audit reconciliation
/// against the rest of the patient record turns into a forensic exercise. Slice J of the
/// SmartConnect ↔ Mirth alignment plan promotes the probe from "detect" to "optionally
/// correct, with an audit trail".
/// </remarks>
public enum ClockSkewCorrectionMode
{
    /// <summary>Default: feed the monitor and leave the message untouched.</summary>
    ReportOnly = 0,

    /// <summary>Feed the monitor and rewrite <c>MSH-7</c> to <see cref="DateTime.UtcNow"/>
    /// when the absolute skew exceeds
    /// <see cref="ClockSkewCorrectionPolicy.CorrectAboveAbsSkew"/> and is still within
    /// <see cref="ClockSkewCorrectionPolicy.MaxAllowedAbsJump"/>. The monitor still records
    /// the *original* skew so the operator dashboard reflects reality, not the corrected
    /// post-state.</summary>
    Normalize = 1,
}
