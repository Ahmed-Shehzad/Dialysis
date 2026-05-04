using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features.RecordSecurityMechanismAssessment;

public sealed class RecordSecurityMechanismAssessmentCommandValidator : AbstractValidator<RecordSecurityMechanismAssessmentCommand>
{
    public RecordSecurityMechanismAssessmentCommandValidator()
    {
        RuleFor(static c => c.MechanismCode, nameof(RecordSecurityMechanismAssessmentCommand.MechanismCode))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 64)
            .WithMessage("MechanismCode is required (max 64).");

        RuleFor(static c => c.AppliedLevel, nameof(RecordSecurityMechanismAssessmentCommand.AppliedLevel))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 32)
            .WithMessage("AppliedLevel is required (max 32).");

        RuleFor(static c => c.Notes, nameof(RecordSecurityMechanismAssessmentCommand.Notes))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 2000)
            .WithMessage("Notes is required (max 2000).");
    }
}
