using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;

/// <summary>
/// Emitted when a receptionist checks in a previously-expected patient. EHR may mirror the
/// patient locally on first sight; PDMS may pre-warm chairside fixtures; HIE may stage a
/// future Encounter export. Carries name + MRN so consumers don't have to fan out for a
/// follow-up lookup before they can render or audit.
/// </summary>
public sealed record PatientCheckedInIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid EntryId,
    Guid PatientId,
    string PatientName,
    string Mrn,
    DateTime CheckedInAtUtc) : IIntegrationEvent;
