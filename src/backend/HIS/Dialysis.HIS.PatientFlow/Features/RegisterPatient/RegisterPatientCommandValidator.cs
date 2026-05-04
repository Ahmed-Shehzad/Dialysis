using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.PatientFlow.Features.RegisterPatient;

public sealed class RegisterPatientCommandValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientCommandValidator()
    {
        RuleFor(static c => c.MedicalRecordNumber, nameof(RegisterPatientCommand.MedicalRecordNumber)).NotEmpty();
    }
}
