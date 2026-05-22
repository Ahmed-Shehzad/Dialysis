namespace Dialysis.SmartConnect.TimeSync;

/// <summary>
/// Per-source rule that <see cref="Hl7V2ClockSkewProbe.TryObserveAndCorrect"/> consults to
/// decide whether (and how aggressively) to rewrite <c>MSH-7</c>. Lives on the flow source
/// connector so operators can dial in a different posture per partner — e.g. a chair-side
/// PCD machine might run in <see cref="ClockSkewCorrectionMode.Normalize"/> while an
/// external lab gateway stays <see cref="ClockSkewCorrectionMode.ReportOnly"/> because its
/// timestamps are downstream-meaningful (e.g. specimen-draw time).
/// </summary>
public sealed record ClockSkewCorrectionPolicy(
    ClockSkewCorrectionMode Mode,
    TimeSpan CorrectAboveAbsSkew,
    TimeSpan MaxAllowedAbsJump)
{
    /// <summary>The default the §2 probe has always used: observe only, never rewrite.</summary>
    public static ClockSkewCorrectionPolicy ReportOnly { get; } =
        new(ClockSkewCorrectionMode.ReportOnly, TimeSpan.Zero, TimeSpan.Zero);

    /// <summary>
    /// Normalise drift between <paramref name="correctAbove"/> and <paramref name="maxAllowed"/>.
    /// Skew below the lower bound is left alone (no point rewriting sub-second drift).
    /// Skew above the upper bound is left alone with a rejection reason in the result —
    /// catastrophic drift (e.g. machine reset to 1970 epoch, or 24h ahead from a misconfig)
    /// is much more likely to be a misconfiguration we'd silently mask than a real reading
    /// to flatten.
    /// </summary>
    public static ClockSkewCorrectionPolicy Normalize(TimeSpan correctAbove, TimeSpan maxAllowed) =>
        new(ClockSkewCorrectionMode.Normalize, correctAbove, maxAllowed);
}
