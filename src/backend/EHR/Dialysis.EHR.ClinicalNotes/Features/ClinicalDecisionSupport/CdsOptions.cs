namespace Dialysis.EHR.ClinicalNotes.Features.ClinicalDecisionSupport;

/// <summary>How an <see cref="AbnormalVitalThreshold"/> trigger compares the latest reading to its target.</summary>
public enum ClinicalComparator
{
    GreaterThan = 1,
    GreaterThanOrEqual = 2,
    LessThan = 3,
    LessThanOrEqual = 4,
}

/// <summary>The kind of condition that fires a <see cref="CdsRule"/>.</summary>
public enum CdsTriggerKind
{
    /// <summary>No result for the expected LOINC within the lookback window.</summary>
    MissingLabWithinMonths = 1,

    /// <summary>The most-recent vital reading meets the (concerning) comparator against the threshold.</summary>
    AbnormalVitalThreshold = 2,

    /// <summary>The patient carries the condition but has no active medication in the expected class.</summary>
    ConditionWithoutMedicationClass = 3,
}

/// <summary>Display severity of a fired recommendation.</summary>
public enum CdsSeverity
{
    Info = 1,
    Warning = 2,
}

/// <summary>Compares a measured value against a target using a <see cref="ClinicalComparator"/>.</summary>
public static class ClinicalComparatorExtensions
{
    public static bool Matches(this ClinicalComparator comparator, decimal value, decimal target) => comparator switch
    {
        ClinicalComparator.GreaterThan => value > target,
        ClinicalComparator.GreaterThanOrEqual => value >= target,
        ClinicalComparator.LessThan => value < target,
        ClinicalComparator.LessThanOrEqual => value <= target,
        _ => false,
    };
}

/// <summary>
/// Config-driven point-of-care clinical decision support, bound from <c>Ehr:Cds</c>. Empty by default →
/// the evaluator is a no-op (the same "off until the practice adopts the rule set" posture as the quality
/// measures and charge edits). Deterministic; no external knowledge base.
/// </summary>
public sealed class CdsOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ehr:Cds";

    /// <summary>The configured rules. Empty → no recommendations are ever raised.</summary>
    public List<CdsRule> Rules { get; } = [];
}

/// <summary>
/// A deterministic, condition-specific decision-support rule. Applies to a patient carrying one of
/// <see cref="AppliesToAnyIcd10"/> on the active problem list (empty → applies to everyone); the
/// <see cref="TriggerKind"/> + its parameters decide whether the recommendation fires.
/// </summary>
public sealed class CdsRule
{
    /// <summary>Stable rule id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable recommendation title (e.g. "Asthma: ensure controller medication").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional supporting detail / rationale.</summary>
    public string? Detail { get; set; }

    /// <summary>Condition ICD-10 codes that make the rule apply (empty → applies to everyone).</summary>
    public List<string> AppliesToAnyIcd10 { get; set; } = [];

    /// <summary>Which trigger evaluates this rule.</summary>
    public CdsTriggerKind TriggerKind { get; set; }

    // MissingLabWithinMonths
    /// <summary>The LOINC the rule expects a recent result for.</summary>
    public string? ExpectedLoinc { get; set; }

    /// <summary>How recent the expected result must be (months). Default 12.</summary>
    public int WithinMonths { get; set; } = 12;

    // AbnormalVitalThreshold
    /// <summary>The vital LOINC to inspect (e.g. systolic BP 8480-6).</summary>
    public string? VitalLoinc { get; set; }

    /// <summary>How the latest reading is compared to <see cref="ThresholdValue"/> to be "concerning".</summary>
    public ClinicalComparator Comparator { get; set; }

    /// <summary>The threshold the latest reading is compared against.</summary>
    public decimal ThresholdValue { get; set; }

    /// <summary>How far back to look for a vital reading (days). Default 180.</summary>
    public int VitalWithinDays { get; set; } = 180;

    // ConditionWithoutMedicationClass
    /// <summary>The rule fires when no active medication's code starts with any of these prefixes.</summary>
    public List<string> MedicationCodePrefixAny { get; set; } = [];

    /// <summary>Display severity of the fired recommendation.</summary>
    public CdsSeverity Severity { get; set; } = CdsSeverity.Info;

    /// <summary>Optional advisory action hint (e.g. "OrderLab", "Prescribe") — display only in v1.</summary>
    public string? SuggestedActionKind { get; set; }

    /// <summary>Optional code for the suggested action (e.g. a LOINC to order).</summary>
    public string? SuggestedActionCode { get; set; }
}
