using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

public sealed record EncounterOpenedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid EncounterId,
    Guid PatientId,
    Guid ProviderId,
    string EncounterClassCode,
    DateTime StartedAtUtc) : IIntegrationEvent;

public sealed record EncounterClosedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid EncounterId,
    Guid PatientId,
    Guid ProviderId,
    DateTime ClosedAtUtc,
    IReadOnlyList<string> DiagnosisIcd10Codes,
    IReadOnlyList<string> ProcedureCptCodes) : IIntegrationEvent;

public sealed record ClinicalNoteSignedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid NoteId,
    Guid EncounterId,
    Guid PatientId,
    Guid SignedByProviderId,
    DateTime SignedAtUtc) : IIntegrationEvent;
