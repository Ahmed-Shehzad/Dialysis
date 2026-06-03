namespace Dialysis.HIE.Documents.Domain;

/// <summary>
/// One signature on a <see cref="DocumentReference"/>. Tracks which cert produced the
/// signature so the audit board and any downstream verifier can recompute trust without
/// having to re-parse the PDF bytes.
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

    private DocumentReferenceSignature() { }

    public DocumentReferenceSignature(
        Guid id,
        Guid documentReferenceId,
        DocumentSignerKind signerKind,
        string certThumbprint,
        DateTime signedAtUtc,
        string? signerUserId = null,
        string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certThumbprint);
        if (signerKind == DocumentSignerKind.User && string.IsNullOrWhiteSpace(signerUserId))
            throw new ArgumentException("Per-user signatures require a SignerUserId.", nameof(signerUserId));

        Id = id == Guid.Empty ? Guid.CreateVersion7() : id;
        DocumentReferenceId = documentReferenceId;
        SignerKind = signerKind;
        SignerUserId = signerUserId;
        CertThumbprint = certThumbprint;
        SignedAtUtc = signedAtUtc;
        Reason = reason;
    }
}
