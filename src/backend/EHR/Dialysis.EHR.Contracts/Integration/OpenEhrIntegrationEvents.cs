using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

/// <summary>
/// Parallel openEHR-shaped projection of a chart vital-sign reading.
///
/// Carries the archetype id + canonical-JSON payload so cross-module consumers
/// (HIE longitudinal store, downstream partners speaking openEHR) can persist the
/// composition without re-deriving the shape from the LOINC-coded source event.
/// </summary>
public sealed record ChartVitalSignProjectedAsOpenEhrIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid VitalSignReadingId,
    Guid PatientId,
    Guid? EncounterId,
    Guid? RecordedByProviderId,
    string ArchetypeId,
    string CompositionJson,
    DateTime ObservedAtUtc) : IIntegrationEvent;

/// <summary>
/// Parallel openEHR-shaped projection of a received lab result, conforming to
/// <c>openEHR-EHR-OBSERVATION.lab_test_result.v1</c>.
/// </summary>
public sealed record LabResultProjectedAsOpenEhrIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid LabResultId,
    Guid LabOrderId,
    Guid PatientId,
    string ArchetypeId,
    string CompositionJson,
    DateTime ObservedAtUtc) : IIntegrationEvent;
