namespace Dialysis.HIE.Inbound.Insights;

/// <summary>
/// A consolidated, cross-source view of what HIE has received about a patient from outside
/// organisations — the "Community Health Record" surface. A projection over current
/// <c>ReceivedResource</c> rows (no event log), computed on demand.
/// </summary>
public sealed record PatientInsightsSummary(
    string PatientReference,
    IReadOnlyList<string> SourceOrganizations,
    DateTime? LastUpdatedUtc,
    InsightsCounts Counts,
    IReadOnlyList<InsightsItem> Recent,
    IReadOnlyList<InsightsItem> Medications,
    IReadOnlyList<InsightsItem> Allergies,
    IReadOnlyList<InsightsItem> Problems,
    IReadOnlyList<DuplicateTestAlert> DuplicateTestAlerts,
    IReadOnlyList<DuplicateMedicationAlert> DuplicateMedicationAlerts,
    IReadOnlyList<AllergyConflictAlert> AllergyConflictAlerts);

/// <summary>Counts of external items by clinical category.</summary>
public sealed record InsightsCounts(
    int Encounters,
    int Observations,
    int Documents,
    int Procedures,
    int Medications,
    int Allergies,
    int Problems,
    int Other,
    int Total);

/// <summary>One external item for the recent-activity strip / a clinical list.</summary>
public sealed record InsightsItem(string ResourceType, DateTime? Date, string SourceOrganization, string? Display);

/// <summary>
/// Same lab observed at more than one source — a duplicate-test signal the clinician can act on
/// (avoid re-ordering, reconcile values).
/// </summary>
public sealed record DuplicateTestAlert(string Code, string? Display, int SourceCount, IReadOnlyList<string> Sources);

/// <summary>Same medication reported by more than one source — a reconciliation signal.</summary>
public sealed record DuplicateMedicationAlert(string Code, string? Display, int SourceCount, IReadOnlyList<string> Sources);

/// <summary>
/// An active external medication that matches a recorded allergy for the patient — a safety signal to
/// reconcile before continuing the medication.
/// </summary>
public sealed record AllergyConflictAlert(string MedicationDisplay, string AllergyDisplay, IReadOnlyList<string> Sources);
