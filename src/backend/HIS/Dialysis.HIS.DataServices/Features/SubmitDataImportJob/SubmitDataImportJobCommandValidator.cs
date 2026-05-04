using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.DataServices.Features.SubmitDataImportJob;

public sealed class SubmitDataImportJobCommandValidator : AbstractValidator<SubmitDataImportJobCommand>
{
    public SubmitDataImportJobCommandValidator()
    {
        RuleFor(static c => c.SourceDescription, nameof(SubmitDataImportJobCommand.SourceDescription))
            .Must(static (_, s) => !string.IsNullOrWhiteSpace(s) && s.Length <= 512)
            .WithMessage("SourceDescription is required (max 512 characters).");
    }
}
