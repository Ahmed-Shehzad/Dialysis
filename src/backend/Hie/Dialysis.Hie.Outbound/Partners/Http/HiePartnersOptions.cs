namespace Dialysis.Hie.Outbound.Partners.Http;

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

    /// <summary>Static bearer token. For OAuth client-credentials, swap to a token provider in a future iteration.</summary>
    public string? BearerToken { get; set; }

    /// <summary>HTTP request timeout in seconds. Default 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
