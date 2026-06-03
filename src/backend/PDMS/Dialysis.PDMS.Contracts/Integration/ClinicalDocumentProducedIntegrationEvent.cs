using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.PDMS.Contracts.Integration;

/// <summary>
/// Published every time PDMS Reporting finalises a clinical-document render (discharge
/// letter, shift report, billing summary). HIE Documents consumes the event and creates a
/// FHIR <c>DocumentReference</c> row pointing at the same <see cref="StorageRef"/>, so the
/// admin Documents view, ePA upload, and partner exchange resolve through one shared blob
/// store. PDMS continues to own the source <c>SessionReport</c> aggregate; the event is
/// a one-way notification, not a transfer of ownership.
/// </summary>
public sealed record ClinicalDocumentProducedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid ReportId,
    Guid PatientId,
    string Kind,
    string MimeType,
    string Title,
    string StorageRef,
    string ContentHash,
    string? LanguageCode) : IIntegrationEvent;
