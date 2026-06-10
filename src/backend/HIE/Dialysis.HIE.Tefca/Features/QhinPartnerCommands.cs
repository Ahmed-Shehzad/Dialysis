using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.CQRS.Queries;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Tefca.Domain;
using Dialysis.HIE.Tefca.Ias;
using Dialysis.HIE.Tefca.Ports;
using Dialysis.HIE.Tefca.Trust;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Tefca.Features;

// -------- Row / detail DTOs --------

public sealed record QhinPartnerRow
{
    public QhinPartnerRow(Guid Id,
        string Name,
        string FhirBaseUrl,
        string IasEndpoint,
        QhinPartnerStatus Status,
        string? MtlsCertThumbprint,
        int TrustAnchorCount,
        DateTime UpdatedAtUtc,
        string UpdatedBy)
    {
        this.Id = Id;
        this.Name = Name;
        this.FhirBaseUrl = FhirBaseUrl;
        this.IasEndpoint = IasEndpoint;
        this.Status = Status;
        this.MtlsCertThumbprint = MtlsCertThumbprint;
        this.TrustAnchorCount = TrustAnchorCount;
        this.UpdatedAtUtc = UpdatedAtUtc;
        this.UpdatedBy = UpdatedBy;
    }
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string FhirBaseUrl { get; init; }
    public string IasEndpoint { get; init; }
    public QhinPartnerStatus Status { get; init; }
    public string? MtlsCertThumbprint { get; init; }
    public int TrustAnchorCount { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public string UpdatedBy { get; init; }
    public void Deconstruct(out Guid id, out string name, out string fhirBaseUrl, out string iasEndpoint, out QhinPartnerStatus status, out string? mtlsCertThumbprint, out int trustAnchorCount, out DateTime updatedAtUtc, out string updatedBy)
    {
        id = Id;
        name = Name;
        fhirBaseUrl = FhirBaseUrl;
        iasEndpoint = IasEndpoint;
        status = Status;
        mtlsCertThumbprint = MtlsCertThumbprint;
        trustAnchorCount = TrustAnchorCount;
        updatedAtUtc = UpdatedAtUtc;
        updatedBy = UpdatedBy;
    }
}

public sealed record QhinTrustAnchorRow
{
    public QhinTrustAnchorRow(Guid Id,
        string Subject,
        string Thumbprint,
        DateTime NotBefore,
        DateTime NotAfter,
        TrustAnchorStatus Status,
        DateTime AttachedAtUtc,
        string AttachedBy)
    {
        this.Id = Id;
        this.Subject = Subject;
        this.Thumbprint = Thumbprint;
        this.NotBefore = NotBefore;
        this.NotAfter = NotAfter;
        this.Status = Status;
        this.AttachedAtUtc = AttachedAtUtc;
        this.AttachedBy = AttachedBy;
    }
    public Guid Id { get; init; }
    public string Subject { get; init; }
    public string Thumbprint { get; init; }
    public DateTime NotBefore { get; init; }
    public DateTime NotAfter { get; init; }
    public TrustAnchorStatus Status { get; init; }
    public DateTime AttachedAtUtc { get; init; }
    public string AttachedBy { get; init; }
    public void Deconstruct(out Guid id, out string subject, out string thumbprint, out DateTime notBefore, out DateTime notAfter, out TrustAnchorStatus status, out DateTime attachedAtUtc, out string attachedBy)
    {
        id = Id;
        subject = Subject;
        thumbprint = Thumbprint;
        notBefore = NotBefore;
        notAfter = NotAfter;
        status = Status;
        attachedAtUtc = AttachedAtUtc;
        attachedBy = AttachedBy;
    }
}

public sealed record QhinPartnerDetail
{
    public QhinPartnerDetail(Guid Id,
        string Name,
        string FhirBaseUrl,
        string IasEndpoint,
        QhinPartnerStatus Status,
        string? MtlsCertThumbprint,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc,
        string UpdatedBy,
        IReadOnlyList<QhinTrustAnchorRow> TrustAnchors)
    {
        this.Id = Id;
        this.Name = Name;
        this.FhirBaseUrl = FhirBaseUrl;
        this.IasEndpoint = IasEndpoint;
        this.Status = Status;
        this.MtlsCertThumbprint = MtlsCertThumbprint;
        this.CreatedAtUtc = CreatedAtUtc;
        this.UpdatedAtUtc = UpdatedAtUtc;
        this.UpdatedBy = UpdatedBy;
        this.TrustAnchors = TrustAnchors;
    }
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string FhirBaseUrl { get; init; }
    public string IasEndpoint { get; init; }
    public QhinPartnerStatus Status { get; init; }
    public string? MtlsCertThumbprint { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public string UpdatedBy { get; init; }
    public IReadOnlyList<QhinTrustAnchorRow> TrustAnchors { get; init; }
    public void Deconstruct(out Guid id, out string name, out string fhirBaseUrl, out string iasEndpoint, out QhinPartnerStatus status, out string? mtlsCertThumbprint, out DateTime createdAtUtc, out DateTime updatedAtUtc, out string updatedBy, out IReadOnlyList<QhinTrustAnchorRow> trustAnchors)
    {
        id = Id;
        name = Name;
        fhirBaseUrl = FhirBaseUrl;
        iasEndpoint = IasEndpoint;
        status = Status;
        mtlsCertThumbprint = MtlsCertThumbprint;
        createdAtUtc = CreatedAtUtc;
        updatedAtUtc = UpdatedAtUtc;
        updatedBy = UpdatedBy;
        trustAnchors = TrustAnchors;
    }
}

// -------- List --------

public sealed record ListQhinPartnersQuery
    : IQuery<IReadOnlyList<QhinPartnerRow>>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TefcaPartnersView;
}

public sealed class ListQhinPartnersQueryHandler : IQueryHandler<ListQhinPartnersQuery, IReadOnlyList<QhinPartnerRow>>
{
    private readonly IQhinPartnerRepository _repository;
    public ListQhinPartnersQueryHandler(IQhinPartnerRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<QhinPartnerRow>> HandleAsync(
        ListQhinPartnersQuery request, CancellationToken cancellationToken)
    {
        var partners = await _repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return [.. partners.Select(p => new QhinPartnerRow(
            p.Id, p.Name, p.FhirBaseUrl, p.IasEndpoint, p.Status,
            p.MtlsCertThumbprint, p.TrustAnchors.Count, p.UpdatedAtUtc, p.UpdatedBy))];
    }
}

// -------- Get --------

public sealed record GetQhinPartnerQuery : IQuery<QhinPartnerDetail?>, IPermissionedCommand
{
    public GetQhinPartnerQuery(Guid Id) => this.Id = Id;
    public string RequiredPermission => HiePermissions.TefcaPartnersView;
    public Guid Id { get; init; }
    public void Deconstruct(out Guid id) => id = Id;
}

public sealed class GetQhinPartnerQueryHandler : IQueryHandler<GetQhinPartnerQuery, QhinPartnerDetail?>
{
    private readonly IQhinPartnerRepository _repository;
    public GetQhinPartnerQueryHandler(IQhinPartnerRepository repository) => _repository = repository;
    public async Task<QhinPartnerDetail?> HandleAsync(GetQhinPartnerQuery request, CancellationToken cancellationToken)
    {
        var partner = await _repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (partner is null) return null;
        return new QhinPartnerDetail(
            partner.Id, partner.Name, partner.FhirBaseUrl, partner.IasEndpoint,
            partner.Status, partner.MtlsCertThumbprint, partner.CreatedAtUtc, partner.UpdatedAtUtc, partner.UpdatedBy,
            [.. partner.TrustAnchors.Select(a => new QhinTrustAnchorRow(
                a.Id, a.Subject, a.Thumbprint, a.NotBefore, a.NotAfter,
                a.Status, a.AttachedAtUtc, a.AttachedBy))]);
    }
}

// -------- Onboard / Revise --------

public sealed record OnboardQhinPartnerCommand : ICommand<Guid>, IPermissionedCommand
{
    public OnboardQhinPartnerCommand(string Name, string FhirBaseUrl, string IasEndpoint, string UpdatedBy)
    {
        this.Name = Name;
        this.FhirBaseUrl = FhirBaseUrl;
        this.IasEndpoint = IasEndpoint;
        this.UpdatedBy = UpdatedBy;
    }
    public string RequiredPermission => HiePermissions.TefcaPartnersAdminister;
    public string Name { get; init; }
    public string FhirBaseUrl { get; init; }
    public string IasEndpoint { get; init; }
    public string UpdatedBy { get; init; }
    public void Deconstruct(out string name, out string fhirBaseUrl, out string iasEndpoint, out string updatedBy)
    {
        name = Name;
        fhirBaseUrl = FhirBaseUrl;
        iasEndpoint = IasEndpoint;
        updatedBy = UpdatedBy;
    }
}

public sealed class OnboardQhinPartnerCommandHandler : ICommandHandler<OnboardQhinPartnerCommand, Guid>
{
    private readonly IQhinPartnerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    public OnboardQhinPartnerCommandHandler(IQhinPartnerRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }
    public async Task<Guid> HandleAsync(OnboardQhinPartnerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var partner = new QhinPartner(
            Guid.CreateVersion7(), request.Name, request.FhirBaseUrl, request.IasEndpoint,
            _clock.GetUtcNow().UtcDateTime, request.UpdatedBy);
        _repository.Add(partner);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return partner.Id;
    }
}

public sealed record ReviseQhinPartnerCommand : ICommand, IPermissionedCommand
{
    public ReviseQhinPartnerCommand(Guid Id, string Name, string FhirBaseUrl, string IasEndpoint, string UpdatedBy)
    {
        this.Id = Id;
        this.Name = Name;
        this.FhirBaseUrl = FhirBaseUrl;
        this.IasEndpoint = IasEndpoint;
        this.UpdatedBy = UpdatedBy;
    }
    public string RequiredPermission => HiePermissions.TefcaPartnersAdminister;
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string FhirBaseUrl { get; init; }
    public string IasEndpoint { get; init; }
    public string UpdatedBy { get; init; }
    public void Deconstruct(out Guid id, out string name, out string fhirBaseUrl, out string iasEndpoint, out string updatedBy)
    {
        id = Id;
        name = Name;
        fhirBaseUrl = FhirBaseUrl;
        iasEndpoint = IasEndpoint;
        updatedBy = UpdatedBy;
    }
}

public sealed class ReviseQhinPartnerCommandHandler : ICommandHandler<ReviseQhinPartnerCommand>
{
    private readonly IQhinPartnerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    public ReviseQhinPartnerCommandHandler(IQhinPartnerRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }
    public async Task<Unit> HandleAsync(ReviseQhinPartnerCommand request, CancellationToken cancellationToken)
    {
        var partner = await _repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{request.Id}' not found.");
        partner.Revise(request.Name, request.FhirBaseUrl, request.IasEndpoint, _clock.GetUtcNow().UtcDateTime, request.UpdatedBy);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// -------- Status transition --------

public sealed record TransitionQhinPartnerStatusCommand : ICommand, IPermissionedCommand
{
    public TransitionQhinPartnerStatusCommand(Guid Id, QhinPartnerStatus Next, string UpdatedBy)
    {
        this.Id = Id;
        this.Next = Next;
        this.UpdatedBy = UpdatedBy;
    }
    public string RequiredPermission => HiePermissions.TefcaPartnersAdminister;
    public Guid Id { get; init; }
    public QhinPartnerStatus Next { get; init; }
    public string UpdatedBy { get; init; }
    public void Deconstruct(out Guid id, out QhinPartnerStatus next, out string updatedBy)
    {
        id = Id;
        next = Next;
        updatedBy = UpdatedBy;
    }
}

public sealed class TransitionQhinPartnerStatusCommandHandler : ICommandHandler<TransitionQhinPartnerStatusCommand>
{
    private readonly IQhinPartnerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    public TransitionQhinPartnerStatusCommandHandler(IQhinPartnerRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }
    public async Task<Unit> HandleAsync(TransitionQhinPartnerStatusCommand request, CancellationToken cancellationToken)
    {
        var partner = await _repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{request.Id}' not found.");
        partner.TransitionStatus(request.Next, _clock.GetUtcNow().UtcDateTime, request.UpdatedBy);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// -------- Attach trust anchor --------

public sealed record AttachTrustAnchorCommand : ICommand<Guid>, IPermissionedCommand
{
    public AttachTrustAnchorCommand(Guid PartnerId, string CertificatePem, string AttachedBy)
    {
        this.PartnerId = PartnerId;
        this.CertificatePem = CertificatePem;
        this.AttachedBy = AttachedBy;
    }
    public string RequiredPermission => HiePermissions.TefcaPartnersAdminister;
    public Guid PartnerId { get; init; }
    public string CertificatePem { get; init; }
    public string AttachedBy { get; init; }
    public void Deconstruct(out Guid partnerId, out string certificatePem, out string attachedBy)
    {
        partnerId = PartnerId;
        certificatePem = CertificatePem;
        attachedBy = AttachedBy;
    }
}

public sealed class AttachTrustAnchorCommandHandler : ICommandHandler<AttachTrustAnchorCommand, Guid>
{
    private readonly IQhinPartnerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    public AttachTrustAnchorCommandHandler(IQhinPartnerRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }
    public async Task<Guid> HandleAsync(AttachTrustAnchorCommand request, CancellationToken cancellationToken)
    {
        var partner = await _repository.FindAsync(request.PartnerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{request.PartnerId}' not found.");
        var parsed = TrustAnchorParser.Parse(request.CertificatePem);
        var anchor = new QhinTrustAnchor(
            Guid.CreateVersion7(), partner.Id,
            parsed.Subject, parsed.Thumbprint, parsed.CertificatePem,
            parsed.NotBefore, parsed.NotAfter,
            _clock.GetUtcNow().UtcDateTime, request.AttachedBy);
        partner.AttachTrustAnchor(anchor);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return anchor.Id;
    }
}

// -------- Revoke trust anchor --------

public sealed record RevokeTrustAnchorCommand : ICommand, IPermissionedCommand
{
    public RevokeTrustAnchorCommand(Guid PartnerId, Guid AnchorId)
    {
        this.PartnerId = PartnerId;
        this.AnchorId = AnchorId;
    }
    public string RequiredPermission => HiePermissions.TefcaPartnersAdminister;
    public Guid PartnerId { get; init; }
    public Guid AnchorId { get; init; }
    public void Deconstruct(out Guid partnerId, out Guid anchorId)
    {
        partnerId = PartnerId;
        anchorId = AnchorId;
    }
}

public sealed class RevokeTrustAnchorCommandHandler : ICommandHandler<RevokeTrustAnchorCommand>
{
    private readonly IQhinPartnerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    public RevokeTrustAnchorCommandHandler(IQhinPartnerRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }
    public async Task<Unit> HandleAsync(RevokeTrustAnchorCommand request, CancellationToken cancellationToken)
    {
        var partner = await _repository.FindAsync(request.PartnerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{request.PartnerId}' not found.");
        partner.RevokeTrustAnchor(request.AnchorId, _clock.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// -------- Rotate mTLS PFX --------

public sealed record RotateMtlsCertificateCommand : ICommand<string>, IPermissionedCommand
{
    public RotateMtlsCertificateCommand(Guid PartnerId, string Base64Pfx, string PfxPassword, string UpdatedBy)
    {
        this.PartnerId = PartnerId;
        this.Base64Pfx = Base64Pfx;
        this.PfxPassword = PfxPassword;
        this.UpdatedBy = UpdatedBy;
    }
    public string RequiredPermission => HiePermissions.TefcaPartnersAdminister;
    public Guid PartnerId { get; init; }
    public string Base64Pfx { get; init; }
    public string PfxPassword { get; init; }
    public string UpdatedBy { get; init; }
    public void Deconstruct(out Guid partnerId, out string base64Pfx, out string pfxPassword, out string updatedBy)
    {
        partnerId = PartnerId;
        base64Pfx = Base64Pfx;
        pfxPassword = PfxPassword;
        updatedBy = UpdatedBy;
    }
}

public sealed class RotateMtlsCertificateCommandHandler : ICommandHandler<RotateMtlsCertificateCommand, string>
{
    private readonly IQhinPartnerRepository _repository;
    private readonly IDocumentBlobStore _blobStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    public RotateMtlsCertificateCommandHandler(IQhinPartnerRepository repository,
        IDocumentBlobStore blobStore,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _repository = repository;
        _blobStore = blobStore;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }
    public async Task<string> HandleAsync(RotateMtlsCertificateCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var partner = await _repository.FindAsync(request.PartnerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{request.PartnerId}' not found.");
        var pfxBytes = Convert.FromBase64String(request.Base64Pfx);
        var thumbprint = ResolveThumbprint(pfxBytes, request.PfxPassword);
        var blobId = Guid.CreateVersion7();
        var storageRef = await _blobStore
            .SaveAsync(blobId, "application/x-pkcs12", pfxBytes, cancellationToken)
            .ConfigureAwait(false);
        partner.RotateMtls(storageRef, thumbprint, _clock.GetUtcNow().UtcDateTime, request.UpdatedBy);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return thumbprint;
    }

    private static string ResolveThumbprint(byte[] pfxBytes, string password)
    {
        try
        {
            var certificate = X509CertificateLoader
                .LoadPkcs12(pfxBytes, password);
            return certificate.Thumbprint;
        }
        catch (CryptographicException ex)
        {
            throw new ArgumentException("Provided mTLS PFX could not be loaded — check the password.", ex);
        }
    }
}

// -------- Issue test IAS JWT --------

public sealed record IssueIasJwtCommand : ICommand<string>, IPermissionedCommand
{
    public IssueIasJwtCommand(Guid PartnerId, string SubjectPatientId, string Scope, int LifetimeSeconds)
    {
        this.PartnerId = PartnerId;
        this.SubjectPatientId = SubjectPatientId;
        this.Scope = Scope;
        this.LifetimeSeconds = LifetimeSeconds;
    }
    public string RequiredPermission => HiePermissions.TefcaIasJwtIssue;
    public Guid PartnerId { get; init; }
    public string SubjectPatientId { get; init; }
    public string Scope { get; init; }
    public int LifetimeSeconds { get; init; }
    public void Deconstruct(out Guid partnerId, out string subjectPatientId, out string scope, out int lifetimeSeconds)
    {
        partnerId = PartnerId;
        subjectPatientId = SubjectPatientId;
        scope = Scope;
        lifetimeSeconds = LifetimeSeconds;
    }
}

public sealed class IssueIasJwtCommandHandler : ICommandHandler<IssueIasJwtCommand, string>
{
    private readonly IQhinPartnerRepository _repository;
    private readonly IIasJwtIssuer _issuer;
    public IssueIasJwtCommandHandler(IQhinPartnerRepository repository, IIasJwtIssuer issuer)
    {
        _repository = repository;
        _issuer = issuer;
    }
    public async Task<string> HandleAsync(IssueIasJwtCommand request, CancellationToken cancellationToken)
    {
        var partner = await _repository.FindAsync(request.PartnerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{request.PartnerId}' not found.");
        return _issuer.Issue(new IasJwtRequest(
            Issuer: "DialysisPlatform.Tefca",
            Audience: partner.IasEndpoint,
            Subject: request.SubjectPatientId,
            Scope: string.IsNullOrWhiteSpace(request.Scope) ? "patient.read" : request.Scope,
            Lifetime: TimeSpan.FromSeconds(Math.Clamp(request.LifetimeSeconds, 60, 3600))));
    }
}
