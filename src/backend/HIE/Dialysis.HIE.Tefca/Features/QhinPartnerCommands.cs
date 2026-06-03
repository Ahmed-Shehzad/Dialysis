using System.Security.Cryptography;
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

public sealed record QhinPartnerRow(
    Guid Id,
    string Name,
    string FhirBaseUrl,
    string IasEndpoint,
    QhinPartnerStatus Status,
    string? MtlsCertThumbprint,
    int TrustAnchorCount,
    DateTime UpdatedAtUtc,
    string UpdatedBy);

public sealed record QhinTrustAnchorRow(
    Guid Id,
    string Subject,
    string Thumbprint,
    DateTime NotBefore,
    DateTime NotAfter,
    TrustAnchorStatus Status,
    DateTime AttachedAtUtc,
    string AttachedBy);

public sealed record QhinPartnerDetail(
    Guid Id,
    string Name,
    string FhirBaseUrl,
    string IasEndpoint,
    QhinPartnerStatus Status,
    string? MtlsCertThumbprint,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string UpdatedBy,
    IReadOnlyList<QhinTrustAnchorRow> TrustAnchors);

// -------- List --------

public sealed record ListQhinPartnersQuery
    : IQuery<IReadOnlyList<QhinPartnerRow>>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TefcaPartnersView;
}

public sealed class ListQhinPartnersQueryHandler(IQhinPartnerRepository repository)
    : IQueryHandler<ListQhinPartnersQuery, IReadOnlyList<QhinPartnerRow>>
{
    public async Task<IReadOnlyList<QhinPartnerRow>> HandleAsync(
        ListQhinPartnersQuery request, CancellationToken cancellationToken)
    {
        var partners = await repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return [.. partners.Select(p => new QhinPartnerRow(
            p.Id, p.Name, p.FhirBaseUrl, p.IasEndpoint, p.Status,
            p.MtlsCertThumbprint, p.TrustAnchors.Count, p.UpdatedAtUtc, p.UpdatedBy))];
    }
}

// -------- Get --------

public sealed record GetQhinPartnerQuery(Guid Id) : IQuery<QhinPartnerDetail?>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TefcaPartnersView;
}

public sealed class GetQhinPartnerQueryHandler(IQhinPartnerRepository repository)
    : IQueryHandler<GetQhinPartnerQuery, QhinPartnerDetail?>
{
    public async Task<QhinPartnerDetail?> HandleAsync(GetQhinPartnerQuery request, CancellationToken cancellationToken)
    {
        var partner = await repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
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

public sealed record OnboardQhinPartnerCommand(string Name, string FhirBaseUrl, string IasEndpoint, string UpdatedBy)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TefcaPartnersAdminister;
}

public sealed class OnboardQhinPartnerCommandHandler(
    IQhinPartnerRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<OnboardQhinPartnerCommand, Guid>
{
    public async Task<Guid> HandleAsync(OnboardQhinPartnerCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var partner = new QhinPartner(
            Guid.CreateVersion7(), request.Name, request.FhirBaseUrl, request.IasEndpoint,
            clock.GetUtcNow().UtcDateTime, request.UpdatedBy);
        repository.Add(partner);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return partner.Id;
    }
}

public sealed record ReviseQhinPartnerCommand(Guid Id, string Name, string FhirBaseUrl, string IasEndpoint, string UpdatedBy)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TefcaPartnersAdminister;
}

public sealed class ReviseQhinPartnerCommandHandler(
    IQhinPartnerRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ReviseQhinPartnerCommand>
{
    public async Task<Unit> HandleAsync(ReviseQhinPartnerCommand request, CancellationToken cancellationToken)
    {
        var partner = await repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{request.Id}' not found.");
        partner.Revise(request.Name, request.FhirBaseUrl, request.IasEndpoint, clock.GetUtcNow().UtcDateTime, request.UpdatedBy);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// -------- Status transition --------

public sealed record TransitionQhinPartnerStatusCommand(Guid Id, QhinPartnerStatus Next, string UpdatedBy)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TefcaPartnersAdminister;
}

public sealed class TransitionQhinPartnerStatusCommandHandler(
    IQhinPartnerRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<TransitionQhinPartnerStatusCommand>
{
    public async Task<Unit> HandleAsync(TransitionQhinPartnerStatusCommand request, CancellationToken cancellationToken)
    {
        var partner = await repository.FindAsync(request.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{request.Id}' not found.");
        partner.TransitionStatus(request.Next, clock.GetUtcNow().UtcDateTime, request.UpdatedBy);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// -------- Attach trust anchor --------

public sealed record AttachTrustAnchorCommand(Guid PartnerId, string CertificatePem, string AttachedBy)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TefcaPartnersAdminister;
}

public sealed class AttachTrustAnchorCommandHandler(
    IQhinPartnerRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<AttachTrustAnchorCommand, Guid>
{
    public async Task<Guid> HandleAsync(AttachTrustAnchorCommand request, CancellationToken cancellationToken)
    {
        var partner = await repository.FindAsync(request.PartnerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{request.PartnerId}' not found.");
        var parsed = TrustAnchorParser.Parse(request.CertificatePem);
        var anchor = new QhinTrustAnchor(
            Guid.CreateVersion7(), partner.Id,
            parsed.Subject, parsed.Thumbprint, parsed.CertificatePem,
            parsed.NotBefore, parsed.NotAfter,
            clock.GetUtcNow().UtcDateTime, request.AttachedBy);
        partner.AttachTrustAnchor(anchor);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return anchor.Id;
    }
}

// -------- Revoke trust anchor --------

public sealed record RevokeTrustAnchorCommand(Guid PartnerId, Guid AnchorId) : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TefcaPartnersAdminister;
}

public sealed class RevokeTrustAnchorCommandHandler(
    IQhinPartnerRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RevokeTrustAnchorCommand>
{
    public async Task<Unit> HandleAsync(RevokeTrustAnchorCommand request, CancellationToken cancellationToken)
    {
        var partner = await repository.FindAsync(request.PartnerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{request.PartnerId}' not found.");
        partner.RevokeTrustAnchor(request.AnchorId, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// -------- Rotate mTLS PFX --------

public sealed record RotateMtlsCertificateCommand(Guid PartnerId, string Base64Pfx, string PfxPassword, string UpdatedBy)
    : ICommand<string>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TefcaPartnersAdminister;
}

public sealed class RotateMtlsCertificateCommandHandler(
    IQhinPartnerRepository repository,
    IDocumentBlobStore blobStore,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RotateMtlsCertificateCommand, string>
{
    public async Task<string> HandleAsync(RotateMtlsCertificateCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var partner = await repository.FindAsync(request.PartnerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{request.PartnerId}' not found.");
        var pfxBytes = Convert.FromBase64String(request.Base64Pfx);
        var thumbprint = ResolveThumbprint(pfxBytes, request.PfxPassword);
        var blobId = Guid.CreateVersion7();
        var storageRef = await blobStore
            .SaveAsync(blobId, "application/x-pkcs12", pfxBytes, cancellationToken)
            .ConfigureAwait(false);
        partner.RotateMtls(storageRef, thumbprint, clock.GetUtcNow().UtcDateTime, request.UpdatedBy);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return thumbprint;
    }

    private static string ResolveThumbprint(byte[] pfxBytes, string password)
    {
        try
        {
            var certificate = System.Security.Cryptography.X509Certificates.X509CertificateLoader
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

public sealed record IssueIasJwtCommand(Guid PartnerId, string SubjectPatientId, string Scope, int LifetimeSeconds)
    : ICommand<string>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.TefcaIasJwtIssue;
}

public sealed class IssueIasJwtCommandHandler(IQhinPartnerRepository repository, IIasJwtIssuer issuer)
    : ICommandHandler<IssueIasJwtCommand, string>
{
    public async Task<string> HandleAsync(IssueIasJwtCommand request, CancellationToken cancellationToken)
    {
        var partner = await repository.FindAsync(request.PartnerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"QHIN partner '{request.PartnerId}' not found.");
        return issuer.Issue(new IasJwtRequest(
            Issuer: "DialysisPlatform.Tefca",
            Audience: partner.IasEndpoint,
            Subject: request.SubjectPatientId,
            Scope: string.IsNullOrWhiteSpace(request.Scope) ? "patient.read" : request.Scope,
            Lifetime: TimeSpan.FromSeconds(Math.Clamp(request.LifetimeSeconds, 60, 3600))));
    }
}
