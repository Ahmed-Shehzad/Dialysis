using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.PatientChart.Domain;

/// <summary>Single vital-signs observation (LOINC-coded).</summary>
public sealed class VitalSignReading : AggregateRoot<Guid>
{
    private VitalSignReading()
    {
    }

    public VitalSignReading(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Guid? EncounterId { get; private set; }

    public Coding ObservationType { get; private set; } = null!;

    public decimal Value { get; private set; }

    public string UnitCode { get; private set; } = string.Empty;

    public DateTime ObservedAtUtc { get; private set; }

    public Guid? RecordedByProviderId { get; private set; }

    public static VitalSignReading Record(
        Guid id,
        Guid patientId,
        Coding observationType,
        decimal value,
        string unitCode,
        DateTime observedAtUtc,
        Guid? encounterId = null,
        Guid? recordedByProviderId = null)
    {
        ArgumentNullException.ThrowIfNull(observationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitCode);
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient id required.", nameof(patientId));

        return new VitalSignReading(id)
        {
            PatientId = patientId,
            EncounterId = encounterId,
            ObservationType = observationType,
            Value = value,
            UnitCode = unitCode.Trim(),
            ObservedAtUtc = observedAtUtc,
            RecordedByProviderId = recordedByProviderId,
        };
    }
}
