using Dialysis.Contracts.Events;
using Verifier;

namespace Dialysis.Prediction.Handlers;

public sealed class ObservationCreatedValidator : AbstractValidator<ObservationCreated>
{
    public ObservationCreatedValidator()
    {
        RuleFor(x => x.ObservationId).Must(id => !string.IsNullOrEmpty(id.Value), "Observation ID is required.");
        RuleFor(x => x.PatientId).Must(id => !string.IsNullOrEmpty(id.Value), "Patient ID is required.");
        RuleFor(x => x.EncounterId).Must(id => !string.IsNullOrEmpty(id.Value), "Encounter ID is required.");
        RuleFor(x => x.Code).NotEmpty("Code is required.");
        RuleFor(x => x.Value).NotEmpty("Value is required.");
    }
}
