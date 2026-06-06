namespace Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;

/// <summary>
/// Config-driven quality / MIPS measures, bound from <c>Ehr:QualityMeasures</c>. Empty by default →
/// the evaluator is a no-op (the same "off until the practice adopts the measure set" posture as the
/// charge edits and reportable codes).
/// </summary>
public sealed class QualityMeasureOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ehr:QualityMeasures";

    /// <summary>The configured measures. Empty → no gaps are ever raised.</summary>
    public List<QualityMeasureRule> Measures { get; } = [];
}

/// <summary>
/// A deterministic care-gap rule: for a patient with one of <see cref="AppliesToAnyIcd10"/> on the
/// active problem list, a lab with <see cref="ExpectedLoinc"/> is expected within
/// <see cref="WithinMonths"/> months; otherwise the measure has an open gap.
/// </summary>
public sealed class QualityMeasureRule
{
    /// <summary>Stable measure id (e.g. a MIPS measure number).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable measure title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Condition ICD-10 codes that make the measure apply (empty → applies to everyone).</summary>
    public List<string> AppliesToAnyIcd10 { get; set; } = [];

    /// <summary>The LOINC the measure expects to see a result for.</summary>
    public string ExpectedLoinc { get; set; } = string.Empty;

    /// <summary>How recent the expected result must be (months). Default 12.</summary>
    public int WithinMonths { get; set; } = 12;
}
