using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.DataServices.Features.ListIntegrationOutboxRecent;

public sealed class ListIntegrationOutboxRecentQueryValidator : AbstractValidator<ListIntegrationOutboxRecentQuery>
{
    public ListIntegrationOutboxRecentQueryValidator() =>
        RuleFor(static c => c.Take, nameof(ListIntegrationOutboxRecentQuery.Take))
            .Must(static (_, t) => t is >= 1 and <= 200)
            .WithMessage("Take must be between 1 and 200.");
}
