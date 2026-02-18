using Verifier;

namespace Dialysis.Prescription.Application.Features.GetPrescriptionByMrn;

internal sealed class GetPrescriptionByMrnQueryValidator : AbstractValidator<GetPrescriptionByMrnQuery>
{
    public GetPrescriptionByMrnQueryValidator()
    {
        _ = RuleFor(x => x.Mrn).NotEmpty("Medical Record Number is required.");
    }
}
