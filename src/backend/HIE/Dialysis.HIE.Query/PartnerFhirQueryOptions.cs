namespace Dialysis.HIE.Query;

/// <summary>Configuration for the outbound partner FHIR query client (<c>Hie:Query</c>).</summary>
public sealed class PartnerFhirQueryOptions
{
    public const string SectionName = "Hie:Query";

    /// <summary>Issuer (<c>iss</c>) asserted in the IAS JWT — our TEFCA participant id.</summary>
    public string IasIssuer { get; set; } = "DialysisPlatform.Tefca";

    /// <summary>IAS scope for a read/query. Cross-org pull is <c>patient.read</c>.</summary>
    public string IasScope { get; set; } = "patient.read";

    /// <summary>IAS JWT lifetime in seconds. Default 300 (5 minutes).</summary>
    public int IasLifetimeSeconds { get; set; } = 300;

    /// <summary>HTTP request timeout in seconds. Default 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
