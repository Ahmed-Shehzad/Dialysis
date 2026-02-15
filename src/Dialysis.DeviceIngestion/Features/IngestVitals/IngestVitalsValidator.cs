using Verifier;

namespace Dialysis.DeviceIngestion.Features.IngestVitals;

public sealed class IngestVitalsValidator : AbstractValidator<IngestVitalsCommand>
{
    public IngestVitalsValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty("Patient ID is required.");
        RuleFor(x => x.EncounterId).NotEmpty("Encounter ID is required.");
        RuleFor(x => x.DeviceId).NotEmpty("Device ID is required.");
        RuleFor(x => x.Readings).NotNull("Readings are required.");
        RuleFor(x => x.Readings).Must(r => r != null && r.Count > 0, "At least one reading is required.");
    }
}
