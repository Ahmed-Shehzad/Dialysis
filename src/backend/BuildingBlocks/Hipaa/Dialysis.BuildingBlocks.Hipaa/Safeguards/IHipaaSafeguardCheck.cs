namespace Dialysis.BuildingBlocks.Hipaa.Safeguards;

/// <summary>
/// One row in the compliance dashboard. Each check reports the live state of a HIPAA Security
/// Rule safeguard so the marketing claim "you don't have to worry about staying up to date" is
/// grounded in something testable instead of marketing copy.
/// </summary>
public interface IHipaaSafeguardCheck
{
    /// <summary>Stable identifier used by tests / dashboards (e.g. <c>encryption-at-rest</c>).</summary>
    string Id { get; }

    /// <summary>Human-readable name surfaced to operators.</summary>
    string Name { get; }

    /// <summary>Which §164.30x family this safeguard sits in.</summary>
    HipaaSafeguardCategory Category { get; }

    /// <summary>Citation in the Security Rule (e.g. <c>§164.312(a)(2)(iv)</c>).</summary>
    string SecurityRuleCitation { get; }

    /// <summary>Evaluate the safeguard now and return its current status.</summary>
    HipaaSafeguardReport Evaluate();
}

/// <summary>Security Rule grouping per 45 CFR 164.308 / 310 / 312 / 314.</summary>
public enum HipaaSafeguardCategory
{
    Administrative,
    Physical,
    Technical,
    Organizational,
}

/// <summary>Outcome the dashboard renders per check.</summary>
public enum HipaaSafeguardStatus
{
    /// <summary>Safeguard is active and verified by this check.</summary>
    Active,
    /// <summary>Safeguard isn't configured. Surfaced as a red row.</summary>
    Missing,
    /// <summary>Configured but reporting degraded state (e.g. ephemeral key ring in production).</summary>
    Degraded,
    /// <summary>The check is irrelevant in this host's profile (e.g. encryption when no PHI is stored).</summary>
    NotApplicable,
}

public sealed record HipaaSafeguardReport(HipaaSafeguardStatus Status, string Evidence);
