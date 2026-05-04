using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.Scheduling.Features.ListSchedulingResources;

public sealed class ListSchedulingResourcesQueryValidator : AbstractValidator<ListSchedulingResourcesQuery>
{
    public ListSchedulingResourcesQueryValidator()
    {
        RuleFor(static q => q.KindCode, nameof(ListSchedulingResourcesQuery.KindCode))
            .Must(static (q, k) => k is null || (k.Length > 0 && k.Length <= 32))
            .WithMessage("KindCode, when set, must be 1–32 characters.");
    }
}
