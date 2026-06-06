namespace Dialysis.EHR.Billing.ChargeEdits;

/// <summary>How strongly a <see cref="ChargeAdvisory"/> should gate billing.</summary>
public enum ChargeAdvisorySeverity
{
    /// <summary>Flagged for the biller's awareness; does not stop the charge/claim.</summary>
    Warning = 1,

    /// <summary>Holds the charge/claim until an authorized user overrides with an audited reason.</summary>
    Blocking = 2,
}

/// <summary>The kind of charge-review edit a <see cref="ChargeAdvisory"/> represents.</summary>
public enum ChargeAdvisoryCategory
{
    /// <summary>This CPT exceeds its configured billable frequency in the window.</summary>
    CptFrequencyLimitExceeded = 1,

    /// <summary>This CPT requires a diagnosis the charge doesn't carry (coverage / medical necessity).</summary>
    MissingRequiredDiagnosis = 2,

    /// <summary>A Medicare non-covered service that needs an Advance Beneficiary Notice before billing.</summary>
    AbnRequired = 3,
}

/// <summary>
/// A single charge-review edit raised before a charge reaches a claim. Deterministic and config-driven —
/// no external coding-edit database.
/// </summary>
public sealed record ChargeAdvisory(
    ChargeAdvisoryCategory Category,
    ChargeAdvisorySeverity Severity,
    string CptCode,
    string? MatchedCode,
    string? Detail);

/// <summary>The edits raised for one (or a set of) charge(s), plus a convenience flag for any blocker.</summary>
public sealed record ChargeAdvisoryResult(IReadOnlyList<ChargeAdvisory> Advisories)
{
    /// <summary>An empty result — no edits raised.</summary>
    public static ChargeAdvisoryResult None { get; } = new([]);

    /// <summary>True when at least one advisory is <see cref="ChargeAdvisorySeverity.Blocking"/>.</summary>
    public bool HasBlocking => Advisories.Any(a => a.Severity == ChargeAdvisorySeverity.Blocking);
}
