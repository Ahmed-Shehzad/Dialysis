using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Dialysis.BuildingBlocks.Documents.Signing;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.SignDocument;

/// <summary>
/// Apply a PAdES-style digital signature to an existing document. The cert source picks
/// between the configured platform cert and the per-user cert resolved via the Identity
/// claim; the result is persisted as a new bytes version and a <c>DocumentReferenceSignature</c>
/// row is appended (signatures stack — multi-sign is supported).
/// </summary>
public sealed record SignDocumentCommand(
    Guid DocumentId,
    PdfSigningCertificateSource CertificateSource,
    string? UserId,
    string? Reason,
    string? Location,
    string? ContactInfo)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.DocumentsSign;
}

public sealed class SignDocumentCommandHandler(
    IDocumentReferenceRepository repository,
    IDocumentBlobStore blobs,
    IPdfSigner signer,
    IEnumerable<ISigningCertificateResolver> resolvers,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<SignDocumentCommand, Guid>
{
    public async Task<Guid> HandleAsync(SignDocumentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var document = await repository.FindAsync(request.DocumentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Document {request.DocumentId} not found.");
        if (!string.Equals(document.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only PDF documents can be digitally signed.");

        var bytes = await blobs.ReadAsync(document.StorageRef, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Blob {document.StorageRef} is missing.");

        var signingRequest = new PdfSigningRequest(
            request.CertificateSource,
            request.UserId,
            request.Reason,
            request.Location,
            request.ContactInfo);

        var signedBytes = await signer.SignAsync(bytes, signingRequest, cancellationToken).ConfigureAwait(false);

        var newRef = await blobs.SaveAsync(Guid.CreateVersion7(), document.MimeType, signedBytes, cancellationToken)
            .ConfigureAwait(false);
        var newHash = Convert.ToHexString(SHA256.HashData(signedBytes));
        document.Revise(newRef, newHash, signedBytes.LongLength, document.HasAcroForms, document.HasJavascript);

        var thumbprint = await ResolveThumbprintAsync(request, resolvers, cancellationToken).ConfigureAwait(false);
        document.RecordSignature(new DocumentReferenceSignature(
            id: Guid.CreateVersion7(),
            documentReferenceId: document.Id,
            signerKind: request.CertificateSource == PdfSigningCertificateSource.Platform
                ? DocumentSignerKind.Platform
                : DocumentSignerKind.User,
            certThumbprint: thumbprint,
            signedAtUtc: clock.GetUtcNow().UtcDateTime,
            signerUserId: request.UserId,
            reason: request.Reason));

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return document.Id;
    }

    private static async Task<string> ResolveThumbprintAsync(
        SignDocumentCommand request,
        IEnumerable<ISigningCertificateResolver> resolvers,
        CancellationToken cancellationToken)
    {
        var resolver = resolvers.FirstOrDefault(r => r.Source == request.CertificateSource);
        if (resolver is null) return string.Empty;
        var certificate = await resolver.ResolveAsync(
            new PdfSigningRequest(request.CertificateSource, request.UserId, request.Reason, request.Location, request.ContactInfo),
            cancellationToken).ConfigureAwait(false);
        return X509CertificateExtensions.GetThumbprint(certificate);
    }
}

internal static class X509CertificateExtensions
{
    public static string GetThumbprint(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        return certificate.Thumbprint;
    }
}
