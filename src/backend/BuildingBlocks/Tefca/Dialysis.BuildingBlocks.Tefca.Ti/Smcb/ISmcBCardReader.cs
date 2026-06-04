namespace Dialysis.BuildingBlocks.Tefca.Ti.Smcb;

/// <summary>
/// PDSG / gematik conformance requires the practice's institutional identity to come from
/// an SMC-B (Security Module Card type B) plugged into a card reader. Crypto operations
/// (signing, key-agreement) run on-card; the application only holds opaque handles.
///
/// The default implementation wraps PC/SC (Personal Computer / Smart Card) — WinSCard on
/// Windows, pcscd on Linux. Tests use <see cref="StubSmcBCardReader"/> or a custom mock.
///
/// In production, the card must remain present in the reader for the duration of the
/// session; <see cref="IsPresent"/> returns false when removed and TI calls fail closed.
/// </summary>
public interface ISmcBCardReader
{
    /// <summary>Whether an SMC-B card is currently inserted in the reader.</summary>
    bool IsPresent { get; }

    /// <summary>
    /// Reads the certificate chain off the card (issuer chain ends at gematik root CA). The
    /// platform uses this to verify the institutional identity against gematik's trust store.
    /// </summary>
    Task<SmcBCertificateChain> ReadCertificateChainAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Asks the card to sign <paramref name="payload"/>. The signature uses the card's
    /// authentication or encryption key (selected via <paramref name="keyKind"/>). The
    /// private key never leaves the card.
    /// </summary>
    Task<byte[]> SignAsync(
        ReadOnlyMemory<byte> payload,
        SmcBKeyKind keyKind,
        CancellationToken cancellationToken);
}

/// <summary>SMC-B key purpose. The card hosts dedicated keys per purpose; the platform must
/// pick the right one for each operation per gematik specs.</summary>
public enum SmcBKeyKind
{
    /// <summary>Practice authentication; used during the gematik IDP token exchange.</summary>
    Authentication,

    /// <summary>Document signing; used for ePA-uploaded documents to prove practice
    /// authorship.</summary>
    Signing,

    /// <summary>Key-agreement / decryption; used to unwrap ePA document keys.</summary>
    KeyAgreement,
}

/// <summary>The SMC-B's certificate chain — leaf at index 0, root at the end.</summary>
public sealed record SmcBCertificateChain
{
    /// <summary>The SMC-B's certificate chain — leaf at index 0, root at the end.</summary>
    public SmcBCertificateChain(IReadOnlyList<byte[]> CertificatesDer) => this.CertificatesDer = CertificatesDer;

    public string ChainFingerprintSha256
    {
        get
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash([.. CertificatesDer.SelectMany(c => c)]);
            return Convert.ToHexString(hash);
        }
    }

    public IReadOnlyList<byte[]> CertificatesDer { get; init; }
    public void Deconstruct(out IReadOnlyList<byte[]> CertificatesDer) => CertificatesDer = this.CertificatesDer;
}
