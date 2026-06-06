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
}
