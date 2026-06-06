namespace Dialysis.EHR.ClinicalNotes.SafetyChecks;

/// <summary>How strongly a <see cref="SafetyAdvisory"/> should gate an order.</summary>
public enum AdvisorySeverity
{
    /// <summary>Surfaced for the clinician's awareness; does not stop the order.</summary>
    Warning = 1,

    /// <summary>Stops the order unless the clinician explicitly acknowledges with an audited reason.</summary>
    Blocking = 2,
}

/// <summary>The kind of safety signal a <see cref="SafetyAdvisory"/> represents.</summary>
public enum AdvisoryCategory
{
    /// <summary>The ordered medication matches a recorded, non-refuted allergy.</summary>
    MedicationAllergyConflict = 1,

    /// <summary>The ordered medication duplicates an active medication statement or prescription.</summary>
    DuplicateActiveMedication = 2,

    /// <summary>The ordered lab panel duplicates a recent, non-cancelled lab order.</summary>
    DuplicateLabOrder = 3,
}

/// <summary>
/// A single point-of-care safety signal raised when an order is checked against the patient's own
/// chart. Deterministic (no external drug-knowledge base): the match is a shared code or a
/// case-insensitive display-substring overlap.
/// </summary>
/// <param name="Category">The kind of signal.</param>
/// <param name="Severity">Whether the signal blocks the order or is advisory only.</param>
/// <param name="MatchedCode">Code of the chart row that matched (allergen / med / LOINC).</param>
/// <param name="MatchedDisplay">Human-readable description of the matched chart row.</param>
/// <param name="OrderedConcept">What the clinician is ordering.</param>
/// <param name="SourceRowId">Id of the matched chart row (allergy / med statement / prescription / lab order).</param>
/// <param name="SourceKind">Which chart aggregate the match came from.</param>
public sealed record SafetyAdvisory(
    AdvisoryCategory Category,
    AdvisorySeverity Severity,
    string MatchedCode,
    string MatchedDisplay,
    string OrderedConcept,
    Guid SourceRowId,
    string SourceKind);

/// <summary>The advisories raised for one order, plus a convenience flag for any blocking signal.</summary>
public sealed record SafetyAdvisoryResult(IReadOnlyList<SafetyAdvisory> Advisories)
{
    /// <summary>An empty result — no advisories raised.</summary>
    public static SafetyAdvisoryResult None { get; } = new([]);

    /// <summary>True when at least one advisory is <see cref="AdvisorySeverity.Blocking"/>.</summary>
    public bool HasBlocking => Advisories.Any(a => a.Severity == AdvisorySeverity.Blocking);
}

/// <summary>
/// Result of placing an order that passed (or overrode) the safety checks: the new aggregate id plus
/// any advisories that were raised (warnings always; blocking advisories only when overridden).
/// </summary>
public sealed record OrderPlacementResult(Guid Id, IReadOnlyList<SafetyAdvisory> Advisories);
