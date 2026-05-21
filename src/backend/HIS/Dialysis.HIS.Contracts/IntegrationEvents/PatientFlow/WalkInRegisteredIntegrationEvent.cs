using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;

/// <summary>
/// Emitted when an unannounced arrival is added to today's queue. Distinct from
/// <see cref="PatientCheckedInIntegrationEvent"/> because there was no prior appointment
/// to expect — downstream modules typically need to create the patient/encounter
/// scaffolding fresh rather than mirror an existing one.
/// </summary>
public sealed record WalkInRegisteredIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid EntryId,
    Guid PatientId,
    string PatientName,
    string Mrn,
    bool EligibilityVerified,
    DateTime RegisteredAtUtc) : IIntegrationEvent;
