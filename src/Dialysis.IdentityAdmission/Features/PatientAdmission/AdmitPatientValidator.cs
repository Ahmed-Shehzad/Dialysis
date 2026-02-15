using Verifier;

namespace Dialysis.IdentityAdmission.Features.PatientAdmission;

public sealed class AdmitPatientValidator : AbstractValidator<AdmitPatientCommand>
{
    public AdmitPatientValidator()
    {
        RuleFor(x => x.Mrn).NotEmpty("MRN is required.");
        RuleFor(x => x.FamilyName).NotEmpty("Family name is required.");
    }
}
