using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.DataServices.Features.GetDataImportJobById;

public sealed class GetDataImportJobByIdQueryValidator : AbstractValidator<GetDataImportJobByIdQuery>
{
    public GetDataImportJobByIdQueryValidator()
    {
        RuleFor(static c => c.Id, nameof(GetDataImportJobByIdQuery.Id))
            .Must(static (_, id) => id != Guid.Empty)
            .WithMessage("Id must be set.");
    }
}
