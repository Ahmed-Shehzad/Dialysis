using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features.RequestAnalyticsExportJob;

public sealed class RequestAnalyticsExportJobCommandValidator : AbstractValidator<RequestAnalyticsExportJobCommand>
{
    public RequestAnalyticsExportJobCommandValidator()
    {
        RuleFor(static c => c.PipelineCode, nameof(RequestAnalyticsExportJobCommand.PipelineCode))
            .Must(static (_, p) => !string.IsNullOrWhiteSpace(p) && p.Length <= 128)
            .WithMessage("PipelineCode is required (max 128 characters).");
    }
}
