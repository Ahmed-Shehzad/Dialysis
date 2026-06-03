namespace Dialysis.BuildingBlocks.Documents.Signing.Csc;

/// <summary>
/// Options bound from configuration for the eIDAS-QES path. Identifies the TSP, the
/// OAuth2 token endpoint used for client-credentials auth, the credential to sign with,
/// and the CSC v2 endpoints. SAD acquisition is a separate exchange and uses the same
/// access token plus the credentialId — see <see cref="CscV2Client"/>.
/// </summary>
public sealed class CscV2Options
{
    /// <summary>Stable identifier for the TSP, persisted on the signature row for audit.</summary>
    public string TspId { get; set; } = string.Empty;

    /// <summary>Base CSC v2 endpoint, e.g. <c>https://sandbox.csc-tsp.example/v2</c>.</summary>
    public string? BaseUri { get; set; }

    /// <summary>OAuth2 client-credentials token endpoint.</summary>
    public string? ClientCredentialsTokenUri { get; set; }

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    /// <summary>Scope requested when fetching the SAD access token.</summary>
    public string Scope { get; set; } = "service";

    /// <summary>Hash algorithm the TSP expects (SHA-256 default).</summary>
    public string HashAlgorithmOid { get; set; } = "2.16.840.1.101.3.4.2.1";

    /// <summary>Signature algorithm the TSP applies (RSA-SHA256 default).</summary>
    public string SignAlgorithmOid { get; set; } = "1.2.840.113549.1.1.11";
}
