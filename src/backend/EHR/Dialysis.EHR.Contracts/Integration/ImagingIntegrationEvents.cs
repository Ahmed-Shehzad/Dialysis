using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

/// <summary>
/// Raised when a clinician orders an imaging study. The imaging modality (PACS/RIS, surfaced through
/// SmartConnect DICOM) fulfils the order and STOWs the resulting study; <see cref="AccessionNumber"/>
/// is the stable correlation id that matches the returned <c>ImagingStudy</c> back to this order.
/// </summary>
public sealed record ImagingOrderPlacedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid ImagingOrderId,
    Guid PatientId,
    Guid EncounterId,
    Guid OrderingProviderId,
    string AccessionNumber,
    string ModalityCode,
    string BodySiteCode,
    string? ReasonText) : IIntegrationEvent;

/// <summary>
/// Raised when an imaging study is correlated back to its order (by accession number) and linked —
/// e.g. after SmartConnect DICOM receives the STOW'd study. EHR records the
/// <see cref="StudyInstanceUid"/> on the order and surfaces it on the chart.
/// </summary>
public sealed record ImagingStudyLinkedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    int SchemaVersion,
    Guid ImagingOrderId,
    Guid PatientId,
    string AccessionNumber,
    string StudyInstanceUid,
    int SeriesCount,
    int InstanceCount) : IIntegrationEvent;
