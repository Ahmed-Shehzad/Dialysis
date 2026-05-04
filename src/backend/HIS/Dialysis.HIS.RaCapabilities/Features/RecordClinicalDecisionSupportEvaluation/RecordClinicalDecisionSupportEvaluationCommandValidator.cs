using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features.RecordClinicalDecisionSupportEvaluation;

public sealed class RecordClinicalDecisionSupportEvaluationCommandValidator
    : AbstractValidator<RecordClinicalDecisionSupportEvaluationCommand>
{
    public RecordClinicalDecisionSupportEvaluationCommandValidator()
    {
        RuleFor(static c => c.PatientId, nameof(RecordClinicalDecisionSupportEvaluationCommand.PatientId))
            .Must(static (_, id) => id != Guid.Empty)
            .WithMessage("PatientId must be set.");
        RuleFor(static c => c.ChecksAppliedJson, nameof(RecordClinicalDecisionSupportEvaluationCommand.ChecksAppliedJson))
            .Must(static (_, j) => !string.IsNullOrWhiteSpace(j) && j.Length <= 8000)
            .WithMessage("ChecksAppliedJson is required (max 8000 characters).");
    }
}
