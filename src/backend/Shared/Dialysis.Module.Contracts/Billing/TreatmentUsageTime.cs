namespace Dialysis.Module.Contracts.Billing;

/// <summary>
/// Formats a dialysis treatment's <em>machine usage time</em> — the wall-clock minutes the
/// machine ran for the session (actual start → completion) — into a compact, human-readable
/// string. Shared across modules so the live chairside timer and every billing / invoice /
/// reporting PDF render the same duration identically.
///
/// Note: with no pause-accounting in the session aggregate today, usage time is the same
/// wall-clock span as the session's actual duration; this helper centralises the wording and
/// formatting so a future pause-aware duration only needs changing in one place.
/// </summary>
public static class TreatmentUsageTime
{
    /// <summary>
    /// Formats <paramref name="minutes"/> as e.g. <c>"3 h 42 min"</c>, <c>"45 min"</c>, or
    /// <c>"2 h"</c>. Negative inputs clamp to zero.
    /// </summary>
    public static string Format(int minutes)
    {
        var total = Math.Max(0, minutes);
        var hours = total / 60;
        var mins = total % 60;
        if (hours == 0)
            return $"{mins} min";
        return mins == 0 ? $"{hours} h" : $"{hours} h {mins} min";
    }
}
