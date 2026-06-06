namespace Dialysis.EHR.ClinicalNotes.SafetyChecks;

/// <summary>
/// Tunables for the point-of-care safety checker, bound from configuration section
/// <c>Ehr:ClinicalSafety</c>. Defaults ship a clinically conservative posture: medication↔allergy
/// conflicts block (override-able), duplicate medications and recently-ordered labs warn.
/// </summary>
public sealed class ClinicalSafetyOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ehr:ClinicalSafety";

    /// <summary>Lookback window (hours) for the duplicate-lab-order check. Default 72h.</summary>
    public int DuplicateLabWindowHours { get; set; } = 72;

    /// <summary>
    /// Whether a medication↔allergy conflict is <see cref="AdvisorySeverity.Blocking"/> (default true)
    /// or a non-blocking warning. Duplicate medication / lab signals are always warnings.
    /// </summary>
    public bool MedicationAllergyConflictBlocks { get; set; } = true;

    /// <summary>
    /// Configured drug↔drug interaction rules, evaluated between the ordered medication and the
    /// patient's current medications. Empty by default (no-op) — populate from configuration or wire a
    /// real interaction provider behind the same <c>IClinicalSafetyChecker</c> seam. Mirrors the
    /// public-health reportable-codes posture: deterministic, config-driven, off until adopted.
    /// </summary>
    public List<DrugInteractionRule> DrugInteractions { get; } = [];
}

/// <summary>
/// A deterministic drug↔drug interaction rule. A medication "matches" a side when its code equals the
/// side's <see cref="Code"/> (case-insensitive) or one display contains the other.
/// </summary>
public sealed class DrugInteractionRule
{
    /// <summary>Code (e.g. RxNorm ingredient) of the first interacting drug.</summary>
    public string FirstCode { get; set; } = string.Empty;

    /// <summary>Optional display of the first interacting drug (enables display-substring matching).</summary>
    public string? FirstDisplay { get; set; }

    /// <summary>Code of the second interacting drug.</summary>
    public string SecondCode { get; set; } = string.Empty;

    /// <summary>Optional display of the second interacting drug.</summary>
    public string? SecondDisplay { get; set; }

    /// <summary>Human-readable description of the interaction, surfaced on the advisory.</summary>
    public string? Description { get; set; }

    /// <summary>When true the interaction blocks (override-able); otherwise it's a warning. Default false.</summary>
    public bool Blocking { get; set; }
}
