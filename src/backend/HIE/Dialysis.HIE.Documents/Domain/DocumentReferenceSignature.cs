namespace Dialysis.HIE.Documents.Domain;

/// <summary>
/// One signature on a <see cref="DocumentReference"/>. Tracks which cert produced the
/// signature so the audit board and any downstream verifier can recompute trust without
/// having to re-parse the PDF bytes. PAdES-LTV fields (<see cref="PadesLevel"/>,
/// <see cref="TsaUri"/>, <see cref="TimestampedAtUtc"/>, <see cref="RevocationEvidenceBlob"/>)
/// are nullable so legacy PAdES-B-B rows from before the LTV upgrade still load; eIDAS-QES
/// fields (<see cref="TspId"/>, <see cref="TspCredentialId"/>) are mandatory when
/// <see cref="SignatureFormat"/> is <see cref="SignatureFormat.Qes"/>.
/// </summary>
public sealed class DocumentReferenceSignature
{
    public Guid Id { get; private set; }
    public Guid DocumentReferenceId { get; private set; }
    public DocumentSignerKind SignerKind { get; private set; }
    public string? SignerUserId { get; private set; }
    public string CertThumbprint { get; private set; } = string.Empty;
    public DateTime SignedAtUtc { get; private set; }
    public string? Reason { get; private set; }

    public PadesLevel PadesLevel { get; private set; }
    public SignatureFormat SignatureFormat { get; private set; }

    // PAdES-T / -LT / -LTA — TSA-stamped time of signing.
    public string? TsaUri { get; private set; }
    public string? TsaCertThumbprint { get; private set; }
    public DateTime? TimestampedAtUtc { get; private set; }

    // PAdES-LT / -LTA — DSS revocation evidence packed into the PDF.
    public RevocationEvidenceFormat RevocationEvidenceFormat { get; private set; }
    public byte[]? RevocationEvidenceBlob { get; private set; }

    // eIDAS-QES — TSP that holds the signing key.
    public string? TspId { get; private set; }
    public string? TspCredentialId { get; private set; }

    private DocumentReferenceSignature() { }

    public DocumentReferenceSignature(
        Guid id,
        Guid documentReferenceId,
        DocumentSignerKind signerKind,
        string certThumbprint,
        DateTime signedAtUtc,
        PadesLevel padesLevel = PadesLevel.B,
        SignatureFormat signatureFormat = SignatureFormat.Aes,
        string? signerUserId = null,
        string? reason = null,
        string? tsaUri = null,
        string? tsaCertThumbprint = null,
        DateTime? timestampedAtUtc = null,
        RevocationEvidenceFormat revocationEvidenceFormat = RevocationEvidenceFormat.None,
        byte[]? revocationEvidenceBlob = null,
        string? tspId = null,
        string? tspCredentialId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certThumbprint);
        if (signerKind == DocumentSignerKind.User && string.IsNullOrWhiteSpace(signerUserId))
            throw new ArgumentException("Per-user signatures require a SignerUserId.", nameof(signerUserId));
        if (signerKind == DocumentSignerKind.RemoteQes && string.IsNullOrWhiteSpace(tspCredentialId))
            throw new ArgumentException("Remote-QES signatures require a TspCredentialId.", nameof(tspCredentialId));
        if (signatureFormat == SignatureFormat.Qes && signerKind != DocumentSignerKind.RemoteQes)
            throw new ArgumentException("QES signatures must use the RemoteQes signer kind.", nameof(signerKind));
        if (padesLevel >= PadesLevel.T && string.IsNullOrWhiteSpace(tsaUri))
            throw new ArgumentException("PAdES-T and higher require a TsaUri.", nameof(tsaUri));
        if (padesLevel >= PadesLevel.LT && revocationEvidenceFormat == RevocationEvidenceFormat.None)
            throw new ArgumentException("PAdES-LT and higher require revocation evidence.", nameof(revocationEvidenceFormat));

        Id = id == Guid.Empty ? Guid.CreateVersion7() : id;
        DocumentReferenceId = documentReferenceId;
        SignerKind = signerKind;
        SignerUserId = signerUserId;
        CertThumbprint = certThumbprint;
        SignedAtUtc = signedAtUtc;
        Reason = reason;
        PadesLevel = padesLevel;
        SignatureFormat = signatureFormat;
        TsaUri = tsaUri;
        TsaCertThumbprint = tsaCertThumbprint;
        TimestampedAtUtc = timestampedAtUtc;
        RevocationEvidenceFormat = revocationEvidenceFormat;
        RevocationEvidenceBlob = revocationEvidenceBlob;
        TspId = tspId;
        TspCredentialId = tspCredentialId;
    }

    /// <summary>
    /// Promotes this signature row from PAdES-B-T (or -B-LT) up to <paramref name="newLevel"/>,
    /// attaching freshly-collected revocation evidence. Used by the async LTV upgrader.
    /// </summary>
    public void UpgradeLevel(PadesLevel newLevel, RevocationEvidenceFormat format, byte[] evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (newLevel <= PadesLevel)
            throw new InvalidOperationException("New PAdES level must be higher than the current level.");
        if (format == RevocationEvidenceFormat.None)
            throw new ArgumentException("Revocation evidence is required when upgrading to LT or higher.", nameof(format));

        PadesLevel = newLevel;
        RevocationEvidenceFormat = format;
        RevocationEvidenceBlob = evidence;
    }
}
