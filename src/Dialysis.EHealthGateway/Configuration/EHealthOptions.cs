namespace Dialysis.EHealthGateway.Configuration;

public sealed class EHealthOptions
{
    public const string SectionName = "EHealth";

    /// <summary>Enabled platform: epa (DE), dmp (FR), spine (UK), or empty to disable.</summary>
    public string? Platform { get; set; }

    /// <summary>Jurisdiction code: DE, FR, UK.</summary>
    public string Jurisdiction { get; set; } = "DE";

    /// <summary>Documents service base URL for fetching DocumentReference content.</summary>
    public string? DocumentsBaseUrl { get; set; }

    /// <summary>FHIR Gateway base URL for DocumentReference/Binary.</summary>
    public string? FhirBaseUrl { get; set; }

    /// <summary>AuditConsent API base URL for consent checks (e.g. https://audit-consent-host). When empty, consent check is skipped.</summary>
    public string? AuditConsentBaseUrl { get; set; }

    /// <summary>Germany (gematik ePA) – set when using certified Konnektor/FdV integration.</summary>
    public EHealthDeOptions? De { get; set; }

    /// <summary>France (DMP) – set when using certified DMP API integration.</summary>
    public EHealthFrOptions? Fr { get; set; }

    /// <summary>UK (NHS Spine) – set when using certified Spine integration.</summary>
    public EHealthUkOptions? Uk { get; set; }
}

/// <summary>Germany – gematik ePA, Konnektor, FdV. Requires certification.</summary>
public sealed class EHealthDeOptions
{
    public string? KonnektorUrl { get; set; }
    public string? FdVBaseUrl { get; set; }
    public string? MandantId { get; set; }
    public string? ClientSystemId { get; set; }
    public string? WorkplaceId { get; set; }
}

/// <summary>France – DMP (Dossier Médical Partagé). Requires certification.</summary>
public sealed class EHealthFrOptions
{
    public string? DmpApiBaseUrl { get; set; }
    public string? ProSanteConnectIssuer { get; set; }
    public string? ClientId { get; set; }
}

/// <summary>UK – NHS Spine. Requires certification.</summary>
public sealed class EHealthUkOptions
{
    public string? SpineBaseUrl { get; set; }
    public string? CareIdentityServiceUrl { get; set; }
    public string? ClientId { get; set; }
}
