using Dialysis.BuildingBlocks.Documents.Signing;
using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIE.Documents.Features.SignDocument;

/// <summary>
/// Validates a PAdES signing request before the (legally significant, irreversible) signature
/// is applied: a target document is required, and the cert source's dependent inputs must be
/// present — a per-user signature needs a UserId, a remote-QES signature needs a TSP credential.
/// </summary>
public sealed class SignDocumentCommandValidator : AbstractValidator<SignDocumentCommand>
{
    public SignDocumentCommandValidator()
    {
        RuleFor(static c => c.DocumentId, nameof(SignDocumentCommand.DocumentId))
            .Must(static (_, v) => v != Guid.Empty)
            .WithMessage("DocumentId is required.");

        RuleFor(static c => c.UserId, nameof(SignDocumentCommand.UserId))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v))
            .When(static c => c.CertificateSource == PdfSigningCertificateSource.User)
            .WithMessage("UserId is required when signing with the per-user certificate source.");

        RuleFor(static c => c.TspCredentialId, nameof(SignDocumentCommand.TspCredentialId))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v))
            .When(static c => c.CertificateSource == PdfSigningCertificateSource.RemoteQes)
            .WithMessage("TspCredentialId is required for a remote-QES signature.");

        RuleFor(static c => c.Reason, nameof(SignDocumentCommand.Reason))
            .Must(static (_, v) => v is null || v.Length <= 512)
            .WithMessage("Reason must be at most 512 characters.");

        RuleFor(static c => c.Location, nameof(SignDocumentCommand.Location))
            .Must(static (_, v) => v is null || v.Length <= 256)
            .WithMessage("Location must be at most 256 characters.");

        RuleFor(static c => c.ContactInfo, nameof(SignDocumentCommand.ContactInfo))
            .Must(static (_, v) => v is null || v.Length <= 256)
            .WithMessage("ContactInfo must be at most 256 characters.");
    }
}
