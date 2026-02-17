using Verifier;

namespace Dialysis.DeviceIngestion.Features.Vitals.Ingest;

public sealed class IngestVitalsValidator : AbstractValidator<IngestVitalsCommand>
{
    public IngestVitalsValidator()
    {
        RuleFor(x => x.PatientId).NotNull();
        RuleFor(x => x.HeartRate).Must(v => !v.HasValue || v.Value is > 0 and <= 300, "HeartRate must be between 0 and 300");
        RuleFor(x => x.WeightKg).Must(v => !v.HasValue || v.Value > 0, "WeightKg must be positive");
        RuleFor(x => x).Must(c =>
                c.BloodPressure is not null || c.HeartRate.HasValue || c.WeightKg.HasValue,
            "At least one vital value (blood pressure, heart rate, or weight) must be provided");
    }
}
