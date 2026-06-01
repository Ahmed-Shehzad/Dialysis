using System.Collections.Frozen;

namespace Dialysis.BuildingBlocks.DataProtection.Retention;

/// <summary>
/// In-memory implementation. The composition root registers the platform-wide retention
/// keys; modules can add their own via <see cref="WithModuleKey"/>.
/// </summary>
public sealed class RetentionSchedule : IRetentionSchedule
{
    private readonly FrozenDictionary<string, RetentionWindowRegistration> _index;

    public RetentionSchedule(IEnumerable<RetentionWindowRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        _index = registrations.ToFrozenDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);
    }

    public RetentionWindow? Resolve(string retentionKey) =>
        _index.TryGetValue(retentionKey, out var reg) ? reg.Window : null;

    public IReadOnlyList<RetentionWindowRegistration> All() => [.. _index.Values];

    /// <summary>
    /// Platform-wide default registrations. Modules add their specific keys via
    /// <see cref="WithModuleKey"/> on the builder.
    /// </summary>
    public static IReadOnlyList<RetentionWindowRegistration> PlatformDefaults() =>
    [
        new(
            "clinical.record",
            new RetentionWindow(
                Minimum: TimeSpan.FromDays(365.25 * 10),
                Maximum: TimeSpan.FromDays(365.25 * 30),
                LegalAuthority: "DE Berufsordnung §10 (10–30 years for clinical records)"),
            "Patient charts, treatment sessions, MAR entries, discharge letters."),
        new(
            "billing.record",
            new RetentionWindow(
                Minimum: TimeSpan.FromDays(365.25 * 10),
                Maximum: TimeSpan.FromDays(365.25 * 10),
                LegalAuthority: "DE HGB §257 (10 years for billing records)"),
            "Claims, EDI 837 exports, charge captures."),
        new(
            "inventory.record",
            new RetentionWindow(
                Minimum: TimeSpan.FromDays(365.25 * 6),
                Maximum: TimeSpan.FromDays(365.25 * 6),
                LegalAuthority: "DE AO §147 (6 years for ancillary commercial records)"),
            "Medication inventory transactions."),
        new(
            "audit.record",
            new RetentionWindow(
                Minimum: TimeSpan.FromDays(365.25 * 3),
                Maximum: TimeSpan.FromDays(365.25 * 10),
                LegalAuthority: "BDSG §22 + GDPR Art. 30 (audit retention)"),
            "Access audit rows, RoPA snapshots, breach-notification logs."),
    ];
}
