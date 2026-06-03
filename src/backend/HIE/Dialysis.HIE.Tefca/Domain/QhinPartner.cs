namespace Dialysis.HIE.Tefca.Domain;

/// <summary>
/// US TEFCA Qualified Health Information Network (QHIN) partner — a remote organisation we
/// exchange clinical documents with through the TEFCA Common Agreement. Each partner has
/// its own FHIR base URL, IAS (Individual Access Services) endpoint, a set of trust
/// anchors (X.509 certs the partner signs with), and mTLS material (PFX stored via the
/// shared blob store) used on every outbound request.
///
/// Aggregate boundary: trust anchors and mTLS-material refs are part of the same
/// consistency boundary as the partner row — the operator never updates one without
/// reviewing the other.
/// </summary>
public sealed class QhinPartner
{
    private readonly List<QhinTrustAnchor> _trustAnchors = [];

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string FhirBaseUrl { get; private set; } = string.Empty;
    public string IasEndpoint { get; private set; } = string.Empty;
    public QhinPartnerStatus Status { get; private set; }
    public string? MtlsCertStorageRef { get; private set; }
    public string? MtlsCertThumbprint { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string UpdatedBy { get; private set; } = string.Empty;

    public IReadOnlyList<QhinTrustAnchor> TrustAnchors => _trustAnchors;

    private QhinPartner() { }

    public QhinPartner(
        Guid id,
        string name,
        string fhirBaseUrl,
        string iasEndpoint,
        DateTime createdAtUtc,
        string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(fhirBaseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(iasEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);
        if (!Uri.TryCreate(fhirBaseUrl, UriKind.Absolute, out _))
            throw new ArgumentException("FhirBaseUrl must be an absolute URL.", nameof(fhirBaseUrl));
        if (!Uri.TryCreate(iasEndpoint, UriKind.Absolute, out _))
            throw new ArgumentException("IasEndpoint must be an absolute URL.", nameof(iasEndpoint));

        Id = id == Guid.Empty ? Guid.CreateVersion7() : id;
        Name = name;
        FhirBaseUrl = fhirBaseUrl;
        IasEndpoint = iasEndpoint;
        Status = QhinPartnerStatus.Onboarding;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        UpdatedBy = updatedBy;
    }

    /// <summary>Operator revised the partner's connection details.</summary>
    public void Revise(string name, string fhirBaseUrl, string iasEndpoint, DateTime now, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(fhirBaseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(iasEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);
        if (!Uri.TryCreate(fhirBaseUrl, UriKind.Absolute, out _))
            throw new ArgumentException("FhirBaseUrl must be an absolute URL.", nameof(fhirBaseUrl));
        if (!Uri.TryCreate(iasEndpoint, UriKind.Absolute, out _))
            throw new ArgumentException("IasEndpoint must be an absolute URL.", nameof(iasEndpoint));

        Name = name;
        FhirBaseUrl = fhirBaseUrl;
        IasEndpoint = iasEndpoint;
        UpdatedAtUtc = now;
        UpdatedBy = updatedBy;
    }

    /// <summary>Flip status — Onboarding → Active when go-live is confirmed; Active → Suspended on issue.</summary>
    public void TransitionStatus(QhinPartnerStatus next, DateTime now, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);
        if (Status == next) return;
        if (next == QhinPartnerStatus.Active && _trustAnchors.Count == 0)
            throw new InvalidOperationException("Cannot activate a QHIN partner without at least one trust anchor.");
        if (next == QhinPartnerStatus.Active && MtlsCertThumbprint is null)
            throw new InvalidOperationException("Cannot activate a QHIN partner without mTLS material on file.");
        Status = next;
        UpdatedAtUtc = now;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Records a new mTLS PFX upload. <paramref name="storageRef"/> is the
    /// <c>IDocumentBlobStore</c> ref the controller resolved when the operator uploaded
    /// the file; <paramref name="thumbprint"/> identifies the cert for the audit trail.
    /// </summary>
    public void RotateMtls(string storageRef, string thumbprint, DateTime now, string updatedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);
        MtlsCertStorageRef = storageRef;
        MtlsCertThumbprint = thumbprint;
        UpdatedAtUtc = now;
        UpdatedBy = updatedBy;
    }

    public void AttachTrustAnchor(QhinTrustAnchor anchor)
    {
        ArgumentNullException.ThrowIfNull(anchor);
        if (_trustAnchors.Any(a => a.Thumbprint == anchor.Thumbprint))
            throw new InvalidOperationException(
                $"Trust anchor with thumbprint '{anchor.Thumbprint}' is already attached.");
        _trustAnchors.Add(anchor);
    }

    public void RevokeTrustAnchor(Guid anchorId, DateTime now)
    {
        var anchor = _trustAnchors.FirstOrDefault(a => a.Id == anchorId)
            ?? throw new InvalidOperationException($"Trust anchor '{anchorId}' is not attached to this partner.");
        anchor.Revoke(now);
    }
}
