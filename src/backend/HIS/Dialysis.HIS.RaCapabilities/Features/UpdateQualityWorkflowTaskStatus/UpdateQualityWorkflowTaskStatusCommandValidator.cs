using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features.UpdateQualityWorkflowTaskStatus;

public sealed class UpdateQualityWorkflowTaskStatusCommandValidator : AbstractValidator<UpdateQualityWorkflowTaskStatusCommand>
{
    private static readonly string[] _allowed =
    [
        "open", "in_progress", "closed", "cancelled",
    ];

    public UpdateQualityWorkflowTaskStatusCommandValidator()
    {
        RuleFor(static c => c.TaskId, nameof(UpdateQualityWorkflowTaskStatusCommand.TaskId))
            .Must(static (_, id) => id != Guid.Empty)
            .WithMessage("TaskId must be set.");

        RuleFor(static c => c.NewStatusCode, nameof(UpdateQualityWorkflowTaskStatusCommand.NewStatusCode))
            .Must(static (_, code) => !string.IsNullOrWhiteSpace(code) && Array.Exists(_allowed, a => string.Equals(a, code.Trim(), StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"NewStatusCode must be one of: {string.Join(", ", _allowed)}.");
    }
}
