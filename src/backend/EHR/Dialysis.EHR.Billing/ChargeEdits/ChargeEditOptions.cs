namespace Dialysis.EHR.Billing.ChargeEdits;

/// <summary>
/// Config-driven charge-review edits, bound from <c>Ehr:Billing:ChargeEdits</c>. Empty by default →
/// the checker is a no-op (the same "off until the practice populates it" posture as the public-health
/// reportable codes and drug-interaction rules).
/// </summary>
public sealed class ChargeEditOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ehr:Billing:ChargeEdits";

    /// <summary>Default rolling window (days) for frequency rules that don't set their own.</summary>
    public int FrequencyWindowDays { get; set; } = 365;

    /// <summary>Per-CPT frequency limits. Empty → no frequency check.</summary>
    public List<CptFrequencyRule> FrequencyLimits { get; } = [];

    /// <summary>Per-CPT required-diagnosis coverage rules. Empty → no coverage check.</summary>
    public List<CptCoverageRule> CoverageRules { get; } = [];

    /// <summary>
    /// Payer codes treated as Medicare for the ABN gate. When a coverage/frequency edit fires for one of
    /// these payers, the advisory is escalated to <see cref="ChargeAdvisoryCategory.AbnRequired"/>. Empty
    /// → no ABN escalation.
    /// </summary>
    public List<string> MedicarePayerCodes { get; } = [];
}

/// <summary>A CPT may be billed at most <see cref="MaxOccurrences"/> times per window.</summary>
public sealed class CptFrequencyRule
{
    public string CptCode { get; set; } = string.Empty;
    public int MaxOccurrences { get; set; }

    /// <summary>Window for this rule; null → use <see cref="ChargeEditOptions.FrequencyWindowDays"/>.</summary>
    public int? WindowDays { get; set; }

    /// <summary>When true the edit blocks (override-able); otherwise a warning. Default false.</summary>
    public bool Blocking { get; set; }
}

/// <summary>A CPT requires at least one of <see cref="RequiredAnyIcd10"/> among its diagnosis pointers.</summary>
public sealed class CptCoverageRule
{
    public string CptCode { get; set; } = string.Empty;
    public List<string> RequiredAnyIcd10 { get; set; } = [];

    /// <summary>When true the edit blocks (override-able); otherwise a warning. Default false.</summary>
    public bool Blocking { get; set; }
}
