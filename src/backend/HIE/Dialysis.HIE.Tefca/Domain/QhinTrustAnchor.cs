namespace Dialysis.HIE.Tefca.Domain;

/// <summary>
/// One X.509 trust anchor (root or intermediate cert) the partner QHIN signs material with.
/// Stored as PEM in <see cref="CertificatePem"/>; the platform's outbound dispatcher
/// re-parses the PEM each time it needs a trust validator (cheap, deterministic, avoids a
/// long-lived <c>X509Certificate2</c> in DI). Each anchor carries the validity window the
/// cert itself declares so the admin UI can warn an operator before an anchor expires.
/// </summary>
public sealed class QhinTrustAnchor
{
    public Guid Id { get; private set; }
    public Guid QhinPartnerId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Thumbprint { get; private set; } = string.Empty;
    public string CertificatePem { get; private set; } = string.Empty;
    public DateTime NotBefore { get; private set; }
    public DateTime NotAfter { get; private set; }
    public DateTime AttachedAtUtc { get; private set; }
    public string AttachedBy { get; private set; } = string.Empty;
    public TrustAnchorStatus Status { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    private QhinTrustAnchor() { }

    public QhinTrustAnchor(
        Guid id,
        Guid qhinPartnerId,
        string subject,
        string thumbprint,
        string certificatePem,
        DateTime notBefore,
        DateTime notAfter,
        DateTime attachedAtUtc,
        string attachedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePem);
        ArgumentException.ThrowIfNullOrWhiteSpace(attachedBy);
        if (notAfter <= notBefore)
            throw new ArgumentException("NotAfter must be later than NotBefore.", nameof(notAfter));

        Id = id == Guid.Empty ? Guid.CreateVersion7() : id;
        QhinPartnerId = qhinPartnerId;
        Subject = subject;
        Thumbprint = thumbprint;
        CertificatePem = certificatePem;
        NotBefore = notBefore;
        NotAfter = notAfter;
        AttachedAtUtc = attachedAtUtc;
        AttachedBy = attachedBy;
        Status = TrustAnchorStatus.Active;
    }

    public void Revoke(DateTime now)
    {
        if (Status == TrustAnchorStatus.Revoked)
            return;
        Status = TrustAnchorStatus.Revoked;
        RevokedAtUtc = now;
    }
}
