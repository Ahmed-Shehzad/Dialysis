namespace Dialysis.BuildingBlocks.DataProtection.BreachNotification;

/// <summary>
/// GDPR Art. 33 requires the controller to notify the supervisory authority within 72 hours
/// of becoming aware of a personal-data breach. Art. 34 requires the data subject to be
/// notified when the breach poses high risk to their rights.
///
/// `IBreachNotifier` writes a structured incident-log entry and raises a
/// <c>BreachDetectedIntegrationEvent</c> so on-call infrastructure can page humans + start
/// the 72-hour clock. Notifying the supervisory authority is a human action — the platform
/// surfaces every breach in `/admin/data-protection/breaches` so the DPO can file the
/// notification before the deadline.
/// </summary>
public interface IBreachNotifier
{
    /// <summary>
    /// Records a breach. Returns the breach record id so the caller can attach context later.
    /// </summary>
    Task<Guid> NotifyAsync(BreachReport report, CancellationToken cancellationToken);
}

/// <summary>One personal-data breach. Pseudonymised where possible.</summary>
public sealed record BreachReport(
    BreachSeverity Severity,
    string ModuleSlug,
    string Summary,
    IReadOnlyList<string> AffectedDataCategories,
    int? AffectedSubjectCount,
    string DetectedBy,
    DateTimeOffset DetectedAtUtc,
    string? ContainmentSummary);

public enum BreachSeverity
{
    /// <summary>Confined to internal access; no exfiltration; no high risk to subjects.
    /// 72-hour clock still starts but Art. 34 individual notification likely not required.</summary>
    Low,

    /// <summary>Moderate risk; likely Art. 33 supervisory notification required.</summary>
    Medium,

    /// <summary>High risk to subject rights / freedoms. Art. 33 + Art. 34 both required.</summary>
    High,

    /// <summary>Confirmed mass exfiltration / public exposure of identifiable health data.</summary>
    Critical,
}
