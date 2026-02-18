using Verifier;

namespace Dialysis.Patient.Application.Features.RegisterPatient;

internal sealed class RegisterPatientCommandValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientCommandValidator()
    {
        _ = RuleFor(x => x.MedicalRecordNumber.Value).NotEmpty("Medical Record Number is required.");
        _ = RuleFor(x => x.Name).NotNull("Name is required.");
        _ = RuleFor(x => x.Name.FirstName).NotEmpty("FirstName is required.");
        _ = RuleFor(x => x.Name.LastName).NotEmpty("LastName is required.");
    }
}
