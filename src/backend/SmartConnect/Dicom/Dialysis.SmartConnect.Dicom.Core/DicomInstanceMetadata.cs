namespace Dialysis.SmartConnect.Dicom;

/// <summary>
/// Identifies a single DICOM SOP Instance within the three-level study → series → instance hierarchy.
/// Each level has a globally-unique UID (per DICOM spec, never to be reused). <see cref="BlobId"/>
/// is the SmartConnect attachment id where the .dcm bytes live.
/// </summary>
public sealed record DicomInstanceMetadata(
    string StudyInstanceUid,
    string SeriesInstanceUid,
    string SopInstanceUid,
    string SopClassUid,
    string? PatientId,
    string? PatientName,
    string? Modality,
    DateTimeOffset ReceivedUtc,
    long SizeBytes,
    Guid BlobId);

/// <summary>
/// Aggregate of one DICOM Study and the series + instances it contains. Built by querying the
/// instance metadata table by <see cref="StudyInstanceUid"/>.
/// </summary>
public sealed record DicomStudy(
    string StudyInstanceUid,
    string? PatientId,
    string? PatientName,
    DateTimeOffset ReceivedUtc,
    IReadOnlyList<DicomSeries> Series);

/// <summary>One series under a study. Instances are leaf .dcm blobs.</summary>
public sealed record DicomSeries(
    string SeriesInstanceUid,
    string? Modality,
    IReadOnlyList<DicomInstanceMetadata> Instances);
