using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

/// <summary>
/// Parallel openEHR-shaped projection of a chart vital-sign reading.
///
/// Carries the archetype id + canonical-JSON payload so cross-module consumers
/// (HIE longitudinal store, downstream partners speaking openEHR) can persist the
/// composition without re-deriving the shape from the LOINC-coded source event.
/// </summary>
public sealed record ChartVitalSignProjectedAsOpenEhrIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Parallel openEHR-shaped projection of a chart vital-sign reading.
    ///
    /// Carries the archetype id + canonical-JSON payload so cross-module consumers
    /// (HIE longitudinal store, downstream partners speaking openEHR) can persist the
    /// composition without re-deriving the shape from the LOINC-coded source event.
    /// </summary>
    public ChartVitalSignProjectedAsOpenEhrIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid VitalSignReadingId,
        Guid PatientId,
        Guid? EncounterId,
        Guid? RecordedByProviderId,
        string ArchetypeId,
        string CompositionJson,
        DateTime ObservedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.VitalSignReadingId = VitalSignReadingId;
        this.PatientId = PatientId;
        this.EncounterId = EncounterId;
        this.RecordedByProviderId = RecordedByProviderId;
        this.ArchetypeId = ArchetypeId;
        this.CompositionJson = CompositionJson;
        this.ObservedAtUtc = ObservedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid VitalSignReadingId { get; init; }
    public Guid PatientId { get; init; }
    public Guid? EncounterId { get; init; }
    public Guid? RecordedByProviderId { get; init; }
    public string ArchetypeId { get; init; }
    public string CompositionJson { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid VitalSignReadingId, out Guid PatientId, out Guid? EncounterId, out Guid? RecordedByProviderId, out string ArchetypeId, out string CompositionJson, out DateTime ObservedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        VitalSignReadingId = this.VitalSignReadingId;
        PatientId = this.PatientId;
        EncounterId = this.EncounterId;
        RecordedByProviderId = this.RecordedByProviderId;
        ArchetypeId = this.ArchetypeId;
        CompositionJson = this.CompositionJson;
        ObservedAtUtc = this.ObservedAtUtc;
    }
}

/// <summary>
/// Parallel openEHR-shaped projection of a received lab result, conforming to
/// <c>openEHR-EHR-OBSERVATION.lab_test_result.v1</c>.
/// </summary>
public sealed record LabResultProjectedAsOpenEhrIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Parallel openEHR-shaped projection of a received lab result, conforming to
    /// <c>openEHR-EHR-OBSERVATION.lab_test_result.v1</c>.
    /// </summary>
    public LabResultProjectedAsOpenEhrIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid LabResultId,
        Guid LabOrderId,
        Guid PatientId,
        string ArchetypeId,
        string CompositionJson,
        DateTime ObservedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.LabResultId = LabResultId;
        this.LabOrderId = LabOrderId;
        this.PatientId = PatientId;
        this.ArchetypeId = ArchetypeId;
        this.CompositionJson = CompositionJson;
        this.ObservedAtUtc = ObservedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid LabResultId { get; init; }
    public Guid LabOrderId { get; init; }
    public Guid PatientId { get; init; }
    public string ArchetypeId { get; init; }
    public string CompositionJson { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid LabResultId, out Guid LabOrderId, out Guid PatientId, out string ArchetypeId, out string CompositionJson, out DateTime ObservedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        LabResultId = this.LabResultId;
        LabOrderId = this.LabOrderId;
        PatientId = this.PatientId;
        ArchetypeId = this.ArchetypeId;
        CompositionJson = this.CompositionJson;
        ObservedAtUtc = this.ObservedAtUtc;
    }
}
