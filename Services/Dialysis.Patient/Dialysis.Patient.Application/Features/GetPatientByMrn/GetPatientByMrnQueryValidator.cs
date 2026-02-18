using Verifier;

namespace Dialysis.Patient.Application.Features.GetPatientByMrn;

internal sealed class GetPatientByMrnQueryValidator : AbstractValidator<GetPatientByMrnQuery>
{
    public GetPatientByMrnQueryValidator()
    {
        _ = RuleFor(x => x.Mrn)
            .NotEmpty("Medical Record Number is required.");
    }
}
