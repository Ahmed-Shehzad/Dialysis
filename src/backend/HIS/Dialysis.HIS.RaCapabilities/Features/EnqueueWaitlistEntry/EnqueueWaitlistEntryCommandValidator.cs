using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features.EnqueueWaitlistEntry;

public sealed class EnqueueWaitlistEntryCommandValidator : AbstractValidator<EnqueueWaitlistEntryCommand>
{
    public EnqueueWaitlistEntryCommandValidator()
    {
        RuleFor(static c => c.PatientId, nameof(EnqueueWaitlistEntryCommand.PatientId))
            .Must(static (_, id) => id != Guid.Empty)
            .WithMessage("PatientId must be set.");
        RuleFor(static c => c.ResourceKindCode, nameof(EnqueueWaitlistEntryCommand.ResourceKindCode))
            .Must(static (_, k) => !string.IsNullOrWhiteSpace(k) && k.Length <= 64)
            .WithMessage("ResourceKindCode is required (max 64 characters).");
        RuleFor(static c => c.Notes, nameof(EnqueueWaitlistEntryCommand.Notes))
            .Must(static (_, n) => n.Length <= 2000)
            .WithMessage("Notes must be at most 2000 characters.");
    }
}
