using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.Operations.Features.GetBillingExportJobById;

public sealed class GetBillingExportJobByIdQueryValidator : AbstractValidator<GetBillingExportJobByIdQuery>
{
    public GetBillingExportJobByIdQueryValidator() =>
        RuleFor(static c => c.Id, nameof(GetBillingExportJobByIdQuery.Id))
            .Must(static (_, id) => id != Guid.Empty)
            .WithMessage("Id must be set.");
}
