namespace Dialysis.BuildingBlocks.DataProtection.Retention;

/// <summary>
/// Per-aggregate retention windows. GDPR Art. 5(1)(e) requires data be kept "no longer than
/// necessary"; German law extends specific minimums (Berufsordnung §10 → 30 years for
/// clinical records; HGB §257 → 10 years for billing; AO §147 → 6 years for ancillary docs).
/// Each module registers its retention keys at startup; the platform-wide
/// `RetentionPrunerHostedService` enforces them.
/// </summary>
public interface IRetentionSchedule
{
    /// <summary>Resolves the retention window for a given key. <c>null</c> = unknown key
    /// (the pruner skips rather than risking a too-aggressive delete).</summary>
    RetentionWindow? Resolve(string retentionKey);

    /// <summary>All registered windows; used by the RoPA generator + DPIA template.</summary>
    IReadOnlyList<RetentionWindowRegistration> All();
}

/// <summary>One retention window: minimum + maximum + the legal authority justifying it.</summary>
public readonly record struct RetentionWindow(
    TimeSpan Minimum,
    TimeSpan Maximum,
    string LegalAuthority);

public sealed record RetentionWindowRegistration(
    string Key,
    RetentionWindow Window,
    string Description);
