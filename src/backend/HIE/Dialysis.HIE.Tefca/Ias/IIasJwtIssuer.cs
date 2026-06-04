namespace Dialysis.HIE.Tefca.Ias;

/// <summary>
/// Issues TEFCA Individual Access Services (IAS) JWTs the operator can hand to a partner
/// QHIN to verify the handshake. The runtime outbound exchange has its own issuance flow
/// (driven by patient-context); this surface ships the *preview / verification* path the
/// admin UI calls when an operator clicks "Issue test IAS JWT".
///
/// Implementation is deliberately HMAC-SHA-256 in v1: the platform owns the secret, the
/// partner verifies with the public copy from the JWKS endpoint the admin UI surfaces.
/// Production deployments swap for RS256 with the platform signing cert (same primitive,
/// different key handle) — the abstraction stays the same.
/// </summary>
public interface IIasJwtIssuer
{
    /// <summary>Mints a JWT carrying the standard TEFCA IAS claims.</summary>
    string Issue(IasJwtRequest request);
}

/// <summary>
/// Inputs to one IAS JWT mint. The audience is the partner QHIN's IAS endpoint host (or
/// its TEFCA-published Participant ID); the subject is the patient on whose behalf the
/// platform is calling. Scope follows the IAS scope catalogue
/// (<c>patient.read</c> for read-only, <c>patient.exchange</c> for cross-org push).
/// </summary>
public sealed record IasJwtRequest
{
    /// <summary>
    /// Inputs to one IAS JWT mint. The audience is the partner QHIN's IAS endpoint host (or
    /// its TEFCA-published Participant ID); the subject is the patient on whose behalf the
    /// platform is calling. Scope follows the IAS scope catalogue
    /// (<c>patient.read</c> for read-only, <c>patient.exchange</c> for cross-org push).
    /// </summary>
    public IasJwtRequest(string Issuer,
        string Audience,
        string Subject,
        string Scope,
        TimeSpan Lifetime)
    {
        this.Issuer = Issuer;
        this.Audience = Audience;
        this.Subject = Subject;
        this.Scope = Scope;
        this.Lifetime = Lifetime;
    }
    public string Issuer { get; init; }
    public string Audience { get; init; }
    public string Subject { get; init; }
    public string Scope { get; init; }
    public TimeSpan Lifetime { get; init; }
    public void Deconstruct(out string Issuer, out string Audience, out string Subject, out string Scope, out TimeSpan Lifetime)
    {
        Issuer = this.Issuer;
        Audience = this.Audience;
        Subject = this.Subject;
        Scope = this.Scope;
        Lifetime = this.Lifetime;
    }
}
