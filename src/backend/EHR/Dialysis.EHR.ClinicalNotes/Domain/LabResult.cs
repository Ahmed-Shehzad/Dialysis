using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

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
        string? referenceRangeText = null,
        string abnormalFlagCode = "")
    {
        if (labOrderId == Guid.Empty)
            throw new ArgumentException("Order required.", nameof(labOrderId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient required.", nameof(patientId));
        ArgumentException.ThrowIfNullOrWhiteSpace(loincCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueText);

        var result = new LabResult(id)
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

        // Raised here, drained to the Transponder outbox by the SaveChanges interceptor in the
        // same transaction as the row — never published manually from a handler. The event keeps
        // the raw HL7 abnormal-flag code (table 0078) the wire delivered, not the mapped enum.
        result.RaiseIntegrationEvent(new LabResultReceivedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            LabResultId: id,
            LabOrderId: labOrderId,
            PatientId: patientId,
            LoincCode: result.LoincCode,
            ValueText: result.ValueText,
            UnitCode: result.UnitCode,
            ReferenceRangeText: result.ReferenceRangeText,
            AbnormalFlag: abnormalFlagCode,
            ObservedAtUtc: observedAtUtc));

        return result;
    }

    /// <summary>
    /// Attaches the openEHR projection of this result, raising the projection integration event
    /// alongside the aggregate so the outbox interceptor dispatches both atomically with the row.
    /// </summary>
    public void RecordOpenEhrProjection(string archetypeId, string compositionJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archetypeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(compositionJson);

        RaiseIntegrationEvent(new LabResultProjectedAsOpenEhrIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            LabResultId: Id,
            LabOrderId: LabOrderId,
            PatientId: PatientId,
            ArchetypeId: archetypeId,
            CompositionJson: compositionJson,
            ObservedAtUtc: ObservedAtUtc));
    }
}
