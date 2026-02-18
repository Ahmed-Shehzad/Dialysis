using Verifier;

namespace Dialysis.Patient.Application.Features.SearchPatients;

internal sealed class SearchPatientsQueryValidator : AbstractValidator<SearchPatientsQuery>
{
    public SearchPatientsQueryValidator()
    {
        _ = RuleFor(x => x.Name).NotNull("Name is required.");
        _ = RuleFor(x => x.Name.FirstName).NotEmpty("FirstName is required.");
        _ = RuleFor(x => x.Name.LastName).NotEmpty("LastName is required.");
    }
}
