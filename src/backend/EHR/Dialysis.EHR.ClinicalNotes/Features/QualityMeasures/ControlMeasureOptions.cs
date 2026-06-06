using Dialysis.EHR.ClinicalNotes.Features.ClinicalDecisionSupport;

namespace Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;

/// <summary>Whether a control measure reads a vital reading or a lab result.</summary>
public enum ControlKind
{
    Vital = 1,
    Lab = 2,
}

/// <summary>
/// Config-driven condition-control measures, bound from <c>Ehr:ControlMeasures</c>. Each rule expresses
/// the *controlled* state (e.g. systolic BP &lt; 140) for a condition cohort; the population query reports
/// the share of the cohort that meets it. Empty by default → no measures.
/// </summary>
public sealed class ControlMeasureOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ehr:ControlMeasures";

    /// <summary>The configured measures. Empty → none.</summary>
    public List<ControlRule> Measures { get; } = [];
}

/// <summary>
/// A deterministic condition-control rule: for a patient carrying one of <see cref="AppliesToAnyIcd10"/>,
/// the most-recent <see cref="Kind"/> reading for <see cref="Code"/> within <see cref="WithinDays"/> is
/// "controlled" when <see cref="Comparator"/> against <see cref="TargetValue"/> holds.
/// </summary>
public sealed class ControlRule
{
    /// <summary>Stable measure id (e.g. "HTN-BP-CONTROL").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable title (e.g. "Hypertension: BP controlled").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Condition ICD-10 codes that put a patient in the measure cohort (empty → everyone).</summary>
    public List<string> AppliesToAnyIcd10 { get; set; } = [];

    /// <summary>Whether the measured value comes from a vital reading or a lab result.</summary>
    public ControlKind Kind { get; set; }

    /// <summary>The LOINC of the vital/lab to inspect.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>How the latest value is compared to <see cref="TargetValue"/> to count as controlled.</summary>
    public ClinicalComparator Comparator { get; set; }

    /// <summary>The control target the latest value is compared against.</summary>
    public decimal TargetValue { get; set; }

    /// <summary>How far back a reading counts (days). Default 180.</summary>
    public int WithinDays { get; set; } = 180;
}
