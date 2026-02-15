using Verifier;

namespace Dialysis.Alerting.Features.ProcessAlerts;

public sealed class AcknowledgeAlertValidator : AbstractValidator<AcknowledgeAlertCommand>
{
    public AcknowledgeAlertValidator()
    {
        RuleFor(x => x.AlertId).NotEmpty("Alert ID is required.");
    }
}
