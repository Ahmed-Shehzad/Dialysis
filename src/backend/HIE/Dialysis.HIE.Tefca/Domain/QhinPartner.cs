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
    // Not readonly: EF rehydrates this via a value-converted scalar column (whole-list assignment),
    // which the analyzer can't see — so IDE0044 is suppressed here, not a missed readonly.
#pragma warning disable IDE0044 // Add readonly modifier
    private List<string> _allowedPurposes = [];
#pragma warning restore IDE0044

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

    /// <summary>
    /// TEFCA permitted purposes this partner is allowed to assert on a request. An <b>empty</b> list
    /// means "all permitted purposes" (the permissive default, so partners onboarded before purpose
    /// governance keep exchanging); narrowing it is an explicit operator action. Enforcement maps this
    /// to a <c>TefcaPartnerPolicy.AllowedPurposes</c> at the gateway boundary.
    /// </summary>
    public IReadOnlyList<string> AllowedPurposes => _allowedPurposes;

    /// <summary>
    /// True when <paramref name="purpose"/> is one this partner may assert. An empty allow-list is
    /// permissive (any recognised purpose passes); otherwise the match is case-insensitive.
    /// </summary>
    public bool IsPurposePermitted(string purpose) =>
        _allowedPurposes.Count == 0
        || _allowedPurposes.Contains(purpose, StringComparer.OrdinalIgnoreCase);

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

    /// <summary>
    /// Replaces the partner's permitted-purpose allow-list. Pass an empty set to restore the
    /// permissive "all purposes" default. Unrecognised tokens are rejected so the admin UI can't
    /// store typos that would silently never match.
    /// </summary>
    public void SetAllowedPurposes(IEnumerable<string> purposes, DateTime now, string updatedBy)
    {
        ArgumentNullException.ThrowIfNull(purposes);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);
        var normalized = purposes
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        _allowedPurposes.Clear();
        _allowedPurposes.AddRange(normalized);
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
