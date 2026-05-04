using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features.ClearPatientAlert;

public sealed class ClearPatientAlertCommandValidator : AbstractValidator<ClearPatientAlertCommand>
{
    public ClearPatientAlertCommandValidator()
    {
        RuleFor(static c => c.AlertId, nameof(ClearPatientAlertCommand.AlertId))
            .Must(static (_, id) => id != Guid.Empty)
            .WithMessage("AlertId must be set.");
    }
}
