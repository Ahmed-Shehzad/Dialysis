using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterResearchEducationActivity;

public sealed class RegisterResearchEducationActivityCommandValidator : AbstractValidator<RegisterResearchEducationActivityCommand>
{
    private static readonly string[] _allowedKinds = ["education", "research"];

    public RegisterResearchEducationActivityCommandValidator()
    {
        RuleFor(static c => c.ActivityKindCode, nameof(RegisterResearchEducationActivityCommand.ActivityKindCode))
            .Must(static (_, k) => !string.IsNullOrWhiteSpace(k) && Array.Exists(_allowedKinds, a => string.Equals(a, k.Trim(), StringComparison.OrdinalIgnoreCase)))
            .WithMessage("ActivityKindCode must be education or research.");

        RuleFor(static c => c.Title, nameof(RegisterResearchEducationActivityCommand.Title))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 256)
            .WithMessage("Title is required (max 256).");

        RuleFor(static c => c.ExternalReference, nameof(RegisterResearchEducationActivityCommand.ExternalReference))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 512)
            .WithMessage("ExternalReference is required (max 512).");
    }
}
