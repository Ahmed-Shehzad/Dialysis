using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.ClinicalNotes.Domain;

public enum LabAbnormalFlag
{
    Normal = 1,
    Low = 2,
    High = 3,
    Critical = 4,
    AbnormalNos = 5,
}

/// <summary>Lab observation result. Created by the Integration BC when an ORU/HL7 result arrives.</summary>
public sealed class LabResult : AggregateRoot<Guid>
{
    private LabResult()
    {
    }

    public LabResult(Guid id) : base(id)
    {
    }

    public Guid LabOrderId { get; private set; }

    public Guid PatientId { get; private set; }

    public string LoincCode { get; private set; } = string.Empty;

    public string ValueText { get; private set; } = string.Empty;

    public string? UnitCode { get; private set; }

    public string? ReferenceRangeText { get; private set; }

    public LabAbnormalFlag AbnormalFlag { get; private set; }

    public DateTime ObservedAtUtc { get; private set; }

    public static LabResult Receive(
        Guid id,
        Guid labOrderId,
        Guid patientId,
        string loincCode,
        string valueText,
        LabAbnormalFlag abnormalFlag,
        DateTime observedAtUtc,
        string? unitCode = null,
        string? referenceRangeText = null)
    {
        if (labOrderId == Guid.Empty) throw new ArgumentException("Order required.", nameof(labOrderId));
        if (patientId == Guid.Empty) throw new ArgumentException("Patient required.", nameof(patientId));
        ArgumentException.ThrowIfNullOrWhiteSpace(loincCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueText);

        return new LabResult(id)
        {
            LabOrderId = labOrderId,
            PatientId = patientId,
            LoincCode = loincCode.Trim(),
            ValueText = valueText.Trim(),
            UnitCode = string.IsNullOrWhiteSpace(unitCode) ? null : unitCode.Trim(),
            ReferenceRangeText = string.IsNullOrWhiteSpace(referenceRangeText) ? null : referenceRangeText.Trim(),
            AbnormalFlag = abnormalFlag,
            ObservedAtUtc = observedAtUtc,
        };
    }
}
