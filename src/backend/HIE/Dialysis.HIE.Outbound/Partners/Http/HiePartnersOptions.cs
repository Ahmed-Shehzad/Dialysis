namespace Dialysis.HIE.Outbound.Partners.Http;

/// <summary>
/// Maps <c>Hie:Partners:&lt;partnerId&gt;:*</c> from configuration to per-partner HTTP settings.
/// Bound via <c>services.Configure&lt;HiePartnersOptions&gt;(config.GetSection("Hie:Partners"))</c> using
/// the dictionary binder.
/// </summary>
public sealed class HiePartnersOptions
{
    public Dictionary<string, PartnerHttpOptions> Partners { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class PartnerHttpOptions
{
    /// <summary>Base URL of the partner's FHIR endpoint (must end with <c>/</c>). Resources are POSTed to <c>{BaseUrl}{ResourceType}</c>.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Static bearer token, used when <see cref="UseIasJwt"/> is false (or no IAS issuer is wired).
    /// Superseded by the per-call TEFCA IAS JWT when IAS is enabled.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>HTTP request timeout in seconds. Default 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// When true, each delivery is authenticated with a freshly minted TEFCA IAS JWT (patient- and
    /// purpose-scoped) instead of the static <see cref="BearerToken"/>. Requires an IAS issuer to be
    /// wired (<c>Tefca:IasJwtIssuer:SigningKey</c>); when none is available the endpoint logs and
    /// falls back to the static token.
    /// </summary>
    public bool UseIasJwt { get; set; }

    /// <summary>
    /// Audience (<c>aud</c>) for the IAS JWT — the partner QHIN's IAS endpoint / participant id.
    /// Falls back to <see cref="BaseUrl"/> when unset.
    /// </summary>
    public string? IasAudience { get; set; }

    /// <summary>Issuer (<c>iss</c>) asserted in the IAS JWT — our TEFCA participant id.</summary>
    public string IasIssuer { get; set; } = "DialysisPlatform.Tefca";

    /// <summary>IAS scope. Outbound cross-org push is <c>patient.exchange</c>.</summary>
    public string IasScope { get; set; } = "patient.exchange";

    /// <summary>IAS JWT lifetime in seconds. Default 300 (5 minutes).</summary>
    public int IasLifetimeSeconds { get; set; } = 300;
}
