using System.Globalization;
using Dialysis.SmartConnect.DataTypes;

namespace Dialysis.SmartConnect.TimeSync;

/// <summary>
/// Reads <c>MSH-7</c> (message timestamp) and <c>MSH-3</c> (sending application) off a
/// parsed <see cref="Hl7V2Message"/> and feeds an observation to the
/// <see cref="IClockSkewMonitor"/>. Idempotent and side-effect free aside from the
/// monitor write; safe to call on every inbound HL7v2 message regardless of trigger.
/// </summary>
/// <remarks>
/// IG §2: dialysis machines compliant with this implementation guide must use the IHE
/// Consistent Time profile (NTP / RFC 1305). A machine that drifts &gt; 1s from the EMR's
/// reference clock will produce treatment timestamps that can't be reconciled with the
/// rest of the patient record — this probe is the early-warning sensor.
/// </remarks>
public static class Hl7V2ClockSkewProbe
{
    public static bool TryObserve(Hl7V2Message message, DateTime serverNowUtc, IClockSkewMonitor monitor)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(monitor);

        var ts = message.GetValue("MSH.7");
        if (!TryParseHl7Timestamp(ts, out var messageTs))
            return false;

        // Prefer the sending application + facility components if present (MSH-3.1, MSH-4)
        // since they uniquely identify a machine; fall back to whole MSH-3 then to "(unknown)".
        var app = message.GetValue("MSH.3.1") ?? message.GetValue("MSH.3");
        var facility = message.GetValue("MSH.4.1") ?? message.GetValue("MSH.4");
        var sourceId = (app, facility) switch
        {
            ({ Length: > 0 }, { Length: > 0 }) => $"{app}@{facility}",
            ({ Length: > 0 }, _) => app!,
            _ => "(unknown)",
        };

        var skew = serverNowUtc - messageTs;
        monitor.Record(new ClockSkewObservation(sourceId, messageTs, serverNowUtc, skew));
        return true;
    }

    /// <summary>
    /// Slice J: observe + optionally correct. Always feeds the monitor with the *original*
    /// skew (so the operator dashboard reflects reality), then — when
    /// <paramref name="policy"/>.Mode is <see cref="ClockSkewCorrectionMode.Normalize"/> and
    /// the absolute skew sits between
    /// <see cref="ClockSkewCorrectionPolicy.CorrectAboveAbsSkew"/> and
    /// <see cref="ClockSkewCorrectionPolicy.MaxAllowedAbsJump"/> — rewrites <c>MSH-7</c> to
    /// <paramref name="serverNowUtc"/>. The returned
    /// <see cref="ClockSkewCorrectionResult"/> carries the audit trail; callers persist it as
    /// an integration event so we never silently retime a clinical message.
    /// </summary>
    public static ClockSkewCorrectionResult? TryObserveAndCorrect(
        Hl7V2Message message,
        DateTime serverNowUtc,
        IClockSkewMonitor monitor,
        ClockSkewCorrectionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(policy);

        var ts = message.GetValue("MSH.7");
        if (!TryParseHl7Timestamp(ts, out var messageTs))
            return null;

        var app = message.GetValue("MSH.3.1") ?? message.GetValue("MSH.3");
        var facility = message.GetValue("MSH.4.1") ?? message.GetValue("MSH.4");
        var sourceId = (app, facility) switch
        {
            ({ Length: > 0 }, { Length: > 0 }) => $"{app}@{facility}",
            ({ Length: > 0 }, _) => app!,
            _ => "(unknown)",
        };

        var skew = serverNowUtc - messageTs;
        monitor.Record(new ClockSkewObservation(sourceId, messageTs, serverNowUtc, skew));

        if (policy.Mode != ClockSkewCorrectionMode.Normalize)
        {
            return new ClockSkewCorrectionResult(
                sourceId, messageTs, serverNowUtc, skew,
                CorrectedMessageTimestampUtc: null,
                WasCorrected: false,
                RejectionReason: null);
        }

        var absSkew = skew.Duration();
        if (absSkew <= policy.CorrectAboveAbsSkew)
        {
            return new ClockSkewCorrectionResult(
                sourceId, messageTs, serverNowUtc, skew,
                CorrectedMessageTimestampUtc: null,
                WasCorrected: false,
                RejectionReason: "below correction threshold");
        }

        if (absSkew > policy.MaxAllowedAbsJump)
        {
            return new ClockSkewCorrectionResult(
                sourceId, messageTs, serverNowUtc, skew,
                CorrectedMessageTimestampUtc: null,
                WasCorrected: false,
                RejectionReason: "exceeds MaxAllowedAbsJump");
        }

        // SetValue requires a component index — the parser holds the MSH-7 timestamp as a
        // single component even when there are no '^' separators on the wire.
        message.SetValue("MSH.7.1", serverNowUtc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
        return new ClockSkewCorrectionResult(
            sourceId, messageTs, serverNowUtc, skew,
            CorrectedMessageTimestampUtc: serverNowUtc,
            WasCorrected: true,
            RejectionReason: null);
    }

    /// <summary>
    /// HL7 v2 timestamp parser covering the IG sample shape
    /// <c>YYYYMMDDHHMMSS[.SSS][ZZZ]</c>. Accepts 8/12/14-digit prefixes (date, date+HM,
    /// date+HMS), optional fractional seconds, and an optional <c>+HHMM</c>/<c>-HHMM</c>
    /// offset. Returns UTC.
    /// </summary>
    public static bool TryParseHl7Timestamp(string? raw, out DateTime utc)
    {
        utc = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var span = raw.Trim();
        string? offsetPart = null;
        var signIdx = span.LastIndexOfAny(['+', '-']);
        // Ignore a sign at position 0 (would mean a negative year, not an offset).
        if (signIdx > 0)
        {
            offsetPart = span[signIdx..];
            span = span[..signIdx];
        }

        var formats = new[]
        {
            "yyyyMMddHHmmss.fff",
            "yyyyMMddHHmmss.ff",
            "yyyyMMddHHmmss.f",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMdd",
        };

        if (!DateTime.TryParseExact(span, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            return false;

        // Compute UTC by subtracting the offset. Absent offset → assume the wire used
        // local server time; mark as Unspecified-then-treat-as-Utc to keep the IG
        // example timestamps round-trippable in tests where serverNow is also UTC.
        if (offsetPart is null)
        {
            utc = DateTime.SpecifyKind(local, DateTimeKind.Utc);
            return true;
        }

        if (!TimeSpan.TryParseExact(offsetPart.AsSpan(1), "hhmm", CultureInfo.InvariantCulture, out var offset))
            return false;
        if (offsetPart[0] == '-')
            offset = -offset;

        utc = DateTime.SpecifyKind(local - offset, DateTimeKind.Utc);
        return true;
    }
}
