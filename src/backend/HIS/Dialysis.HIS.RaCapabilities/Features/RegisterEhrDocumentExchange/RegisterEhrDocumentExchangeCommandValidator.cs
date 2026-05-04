using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterEhrDocumentExchange;

public sealed class RegisterEhrDocumentExchangeCommandValidator : AbstractValidator<RegisterEhrDocumentExchangeCommand>
{
    public RegisterEhrDocumentExchangeCommandValidator()
    {
        RuleFor(static c => c.PatientId, nameof(RegisterEhrDocumentExchangeCommand.PatientId))
            .Must(static (_, id) => id != Guid.Empty)
            .WithMessage("PatientId must be set.");

        RuleFor(static c => c.DocumentTypeCode, nameof(RegisterEhrDocumentExchangeCommand.DocumentTypeCode))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 64)
            .WithMessage("DocumentTypeCode is required (max 64).");

        RuleFor(static c => c.ExternalSystemCode, nameof(RegisterEhrDocumentExchangeCommand.ExternalSystemCode))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 64)
            .WithMessage("ExternalSystemCode is required (max 64).");

        RuleFor(static c => c.ExternalUri, nameof(RegisterEhrDocumentExchangeCommand.ExternalUri))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 1024)
            .WithMessage("ExternalUri is required (max 1024).");
    }
}
