using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIE.Documents.Features.UploadDocument;

/// <summary>
/// Validates an operator document upload before bytes are decoded and persisted: a patient,
/// kind, title and MIME type are required, and the base64 payload must be non-empty and
/// actually decodable so a malformed upload fails fast instead of throwing deep in the handler.
/// </summary>
public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(static c => c.PatientId, nameof(UploadDocumentCommand.PatientId))
            .Must(static (_, v) => v != Guid.Empty)
            .WithMessage("PatientId is required.");

        RuleFor(static c => c.Kind, nameof(UploadDocumentCommand.Kind))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && v.Length <= 128)
            .WithMessage("Kind is required and must be at most 128 characters.");

        RuleFor(static c => c.Title, nameof(UploadDocumentCommand.Title))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && v.Length <= 512)
            .WithMessage("Title is required and must be at most 512 characters.");

        RuleFor(static c => c.MimeType, nameof(UploadDocumentCommand.MimeType))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && v.Length <= 256)
            .WithMessage("MimeType is required and must be at most 256 characters.");

        RuleFor(static c => c.Base64Content, nameof(UploadDocumentCommand.Base64Content))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && IsBase64(v))
            .WithMessage("Base64Content is required and must be valid base64.");

        RuleFor(static c => c.LanguageCode, nameof(UploadDocumentCommand.LanguageCode))
            .Must(static (_, v) => v is null || v.Length <= 16)
            .WithMessage("LanguageCode must be at most 16 characters.");

        RuleFor(static c => c.Category, nameof(UploadDocumentCommand.Category))
            .Must(static (_, v) => v is null || v.Length <= 128)
            .WithMessage("Category must be at most 128 characters.");

        RuleFor(static c => c.CreatedBy, nameof(UploadDocumentCommand.CreatedBy))
            .Must(static (_, v) => v is null || v.Length <= 128)
            .WithMessage("CreatedBy must be at most 128 characters.");
    }

    private static bool IsBase64(string value)
    {
        Span<byte> buffer = value.Length <= 1024 ? stackalloc byte[value.Length] : new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }
}
