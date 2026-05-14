using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;

/// <summary>
/// Emitted when a patient is admitted to a ward in HIS patient-flow. Consumed by downstream care-coordination
/// and analytics sub-modules; EHR's encounter aggregate links the admission via PatientId + AdmittedAtUtc.
/// </summary>
public sealed record PatientAdmittedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid AdmissionId,
    Guid PatientId,
    string WardCode,
    DateTime AdmittedAtUtc) : IIntegrationEvent;
