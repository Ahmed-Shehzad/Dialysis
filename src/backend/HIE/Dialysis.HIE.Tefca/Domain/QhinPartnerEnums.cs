namespace Dialysis.HIE.Tefca.Domain;

/// <summary>Operator-driven lifecycle of a QHIN partner.</summary>
public enum QhinPartnerStatus
{
    /// <summary>Operator added the partner row; trust anchors / mTLS not yet on file.</summary>
    Onboarding = 1,

    /// <summary>Partner is wired for outbound exchange.</summary>
    Active = 2,

    /// <summary>Operator paused exchange (cert rotation in flight, incident, etc.). Partner row preserved.</summary>
    Suspended = 3,
}

/// <summary>Status of an individual trust anchor.</summary>
public enum TrustAnchorStatus
{
    Active = 1,
    Revoked = 2,
}
