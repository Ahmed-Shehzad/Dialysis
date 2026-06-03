namespace Dialysis.BuildingBlocks.Documents.Signing.Ltv;

/// <summary>
/// Options for the RFC 3161 Time-Stamp Authority the signer asks for a signed timestamp
/// from. PDFsharp's <c>PdfSharpDefaultSigner</c> takes the URI in its ctor and fetches the
/// timestamp itself; we pass the configured URI through. Hosts that don't configure a TSA
/// can still produce PAdES-B-B signatures.
/// </summary>
public sealed class TsaOptions
{
    /// <summary>RFC 3161 TSA endpoint (e.g. <c>http://timestamp.digicert.com</c>). Optional.</summary>
    public string? Uri { get; set; }

    /// <summary>Optional HTTP Basic credentials for TSAs that require them.</summary>
    public string? Username { get; set; }
    public string? Password { get; set; }
}

/// <summary>
/// Options for the long-term-validation augmenter — controls whether the augmenter runs
/// at all, whether the background upgrader is enabled, and how aggressively to fetch
/// revocation evidence.
/// </summary>
public sealed class LtvOptions
{
    /// <summary>
    /// When true, the <c>PdfSharpLtvAugmenter</c> opens the just-signed PDF and writes a
    /// DSS dictionary into the catalog so the signature is long-term verifiable. Default
    /// off so hosts that don't yet configure a TSA / revocation source aren't surprised.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// When true, the host registers <c>LtvUpgraderHostedService</c> so already-signed
    /// PAdES-B-T documents are walked daily and promoted to -B-LTA before the TSA cert
    /// expires. Disabled by default for v1 — opt in once the operator is comfortable
    /// with a background mutator.
    /// </summary>
    public bool AutoUpgrade { get; set; }

    /// <summary>Maximum wall-clock seconds the augmenter waits on a CRL or OCSP fetch.</summary>
    public int FetchTimeoutSeconds { get; set; } = 10;
}
