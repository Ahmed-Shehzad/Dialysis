using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.DataServices.Features.SearchPatients;

public sealed class SearchPatientsQueryValidator : AbstractValidator<SearchPatientsQuery>
{
    public SearchPatientsQueryValidator()
    {
        RuleFor(static q => q.Take, nameof(SearchPatientsQuery.Take))
            .Must(static (_, v) => v is > 0 and <= 200)
            .WithMessage("Take must be between 1 and 200.");

        RuleFor(static q => q.Skip, nameof(SearchPatientsQuery.Skip))
            .Must(static (_, v) => v >= 0)
            .WithMessage("Skip must be greater than or equal to 0.");
    }
}
