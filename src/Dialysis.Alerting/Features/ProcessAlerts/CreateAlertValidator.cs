using Verifier;

namespace Dialysis.Alerting.Features.ProcessAlerts;

public sealed class CreateAlertValidator : AbstractValidator<CreateAlertCommand>
{
    public CreateAlertValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty("Patient ID is required.");
        RuleFor(x => x.EncounterId).NotEmpty("Encounter ID is required.");
        RuleFor(x => x.Code).NotEmpty("Code is required.");
        RuleFor(x => x.Severity).NotEmpty("Severity is required.");
    }
}
