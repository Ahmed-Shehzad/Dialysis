using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterSpecialistEncounter;

public sealed class RegisterSpecialistEncounterCommandValidator : AbstractValidator<RegisterSpecialistEncounterCommand>
{
    public RegisterSpecialistEncounterCommandValidator()
    {
        RuleFor(static c => c.PatientId, nameof(RegisterSpecialistEncounterCommand.PatientId))
            .Must(static (_, id) => id != Guid.Empty)
            .WithMessage("PatientId must be set.");

        RuleFor(static c => c.SpecialtyCode, nameof(RegisterSpecialistEncounterCommand.SpecialtyCode))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 64)
            .WithMessage("SpecialtyCode is required (max 64).");

        RuleFor(static c => c.ExternalSystemCode, nameof(RegisterSpecialistEncounterCommand.ExternalSystemCode))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 64)
            .WithMessage("ExternalSystemCode is required (max 64).");

        RuleFor(static c => c.Summary, nameof(RegisterSpecialistEncounterCommand.Summary))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Trim().Length <= 2000)
            .WithMessage("Summary is required (max 2000).");
    }
}
