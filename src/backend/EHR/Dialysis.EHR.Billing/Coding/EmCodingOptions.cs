namespace Dialysis.EHR.Billing.Coding;

/// <summary>
/// Config-driven evaluation-and-management (E/M) coding rules, bound from <c>Ehr:Billing:EmCoding</c>.
/// Each rule maps a documentation threshold (problems addressed + data reviewed) to an E/M CPT level.
/// Empty → no suggestion (the same off-until-adopted posture as the charge edits).
/// </summary>
public sealed class EmCodingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ehr:Billing:EmCoding";

    /// <summary>The configured E/M levels, ascending or any order (the coder sorts by level).</summary>
    public List<EmLevelRule> Levels { get; } = [];

    /// <summary>The CPT codes considered E/M for under-coding detection; empty → derived from <see cref="Levels"/>.</summary>
    public List<string> EmCptCodes { get; } = [];
}

/// <summary>
/// One E/M level: the visit qualifies for <see cref="CptCode"/> (= <see cref="Level"/>) when at least
/// <see cref="MinDiagnoses"/> problems are addressed and <see cref="MinDataReviewed"/> data elements
/// reviewed. Deterministic; a simplified proxy for medical-decision-making complexity.
/// </summary>
public sealed class EmLevelRule
{
    /// <summary>The E/M CPT code (e.g. "99214").</summary>
    public string CptCode { get; set; } = string.Empty;

    /// <summary>The numeric level used to compare codes (e.g. 99213 → 3, 99214 → 4, 99215 → 5).</summary>
    public int Level { get; set; }

    /// <summary>Minimum number of problems addressed for this level.</summary>
    public int MinDiagnoses { get; set; }

    /// <summary>Minimum number of data elements reviewed for this level.</summary>
    public int MinDataReviewed { get; set; }

    /// <summary>Plain-language rationale shown with the suggestion.</summary>
    public string Rationale { get; set; } = string.Empty;
}
