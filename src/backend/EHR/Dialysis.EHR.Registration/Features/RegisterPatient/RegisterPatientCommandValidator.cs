using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.EHR.Registration.Features.RegisterPatient;

public sealed class RegisterPatientCommandValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientCommandValidator()
    {
        RuleFor(static c => c.MedicalRecordNumber, nameof(RegisterPatientCommand.MedicalRecordNumber))
            .Must(static (_, mrn) => !string.IsNullOrWhiteSpace(mrn) && mrn.Trim().Length <= 64);
        RuleFor(static c => c.FamilyName, nameof(RegisterPatientCommand.FamilyName))
            .Must(static (_, n) => !string.IsNullOrWhiteSpace(n) && n.Trim().Length <= 128);
        RuleFor(static c => c.GivenName, nameof(RegisterPatientCommand.GivenName))
            .Must(static (_, n) => !string.IsNullOrWhiteSpace(n) && n.Trim().Length <= 128);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var earliest = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-150));
        RuleFor(static c => c.DateOfBirth, nameof(RegisterPatientCommand.DateOfBirth))
            .Must((_, dob) => dob <= today && dob >= earliest);
    }
}
