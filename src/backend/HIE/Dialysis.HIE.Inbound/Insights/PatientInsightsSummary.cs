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
    IReadOnlyList<DuplicateTestAlert> DuplicateTestAlerts);

/// <summary>Counts of external items by clinical category.</summary>
public sealed record InsightsCounts(int Encounters, int Observations, int Documents, int Procedures, int Other, int Total);

/// <summary>One external item for the recent-activity strip.</summary>
public sealed record InsightsItem(string ResourceType, DateTime? Date, string SourceOrganization, string? Display);

/// <summary>
/// Same lab observed at more than one source — a duplicate-test signal the clinician can act on
/// (avoid re-ordering, reconcile values).
/// </summary>
public sealed record DuplicateTestAlert(string Code, string? Display, int SourceCount, IReadOnlyList<string> Sources);
