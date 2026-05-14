namespace Dialysis.BuildingBlocks.Fhir.Terminology;

/// <summary>
/// Configures the upstream FHIR terminology server and the in-process TTL cache that fronts it.
/// Bind to <c>&lt;Module&gt;:Fhir:Terminology</c> in each module host.
/// </summary>
public sealed class FhirTerminologyOptions
{
    /// <summary>Default development endpoint — HL7's public FHIR R4 test terminology server.</summary>
    public const string TxFhirOrgR4 = "https://tx.fhir.org/r4";

    /// <summary>Base URL of the upstream FHIR terminology server. Operations are appended (e.g. <c>/CodeSystem/$lookup</c>).</summary>
    public string Endpoint { get; set; } = TxFhirOrgR4;

    /// <summary>Optional bearer token sent on every request (some servers — e.g. Ontoserver — require auth).</summary>
    public string? BearerToken { get; set; }

    /// <summary>Request timeout for upstream calls. Lookups are typically &lt;200ms; pad for cold expansions.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Transient-failure retry attempts (HttpRequestException / 5xx). Polly exponential-backoff base 200ms.</summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>Cache TTL for <c>$lookup</c> results (concept properties are stable).</summary>
    public TimeSpan LookupCacheTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Cache TTL for <c>$validate-code</c> results — same lifetime as lookup since both are version-stable.</summary>
    public TimeSpan ValidateCodeCacheTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Cache TTL for <c>$translate</c> results.</summary>
    public TimeSpan TranslateCacheTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Cache TTL for <c>$expand</c> results. Expansions can be large; keep TTL conservative.</summary>
    public TimeSpan ExpandCacheTtl { get; set; } = TimeSpan.FromHours(6);

    /// <summary>Total entries the in-memory cache will hold. <c>0</c> disables caching.</summary>
    public int CacheSizeLimit { get; set; } = 10_000;
}
