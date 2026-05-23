namespace Dialysis.SmartConnect.Dicom;

/// <summary>
/// Persistence seam for DICOM instance metadata. The .dcm bytes themselves live in the
/// SmartConnect attachment blob store; this store keeps the searchable headers (UIDs, patient,
/// modality, dates) for QIDO-RS queries (PR 12).
/// </summary>
public interface IDicomInstanceStore
{
    Task AddAsync(DicomInstanceMetadata metadata, CancellationToken cancellationToken);

    Task<DicomInstanceMetadata?> GetAsync(string sopInstanceUid, CancellationToken cancellationToken);

    /// <summary>Returns every instance under a study, ordered by series UID then SOP UID.</summary>
    Task<IReadOnlyList<DicomInstanceMetadata>> GetByStudyAsync(
        string studyInstanceUid, CancellationToken cancellationToken);

    /// <summary>
    /// QIDO-RS study-level search. Returns the aggregated study list matching the optional
    /// patient / study-date filters; <c>null</c> filters match everything.
    /// </summary>
    Task<IReadOnlyList<DicomStudy>> SearchStudiesAsync(
        string? patientId,
        DateTimeOffset? studyDateFrom,
        DateTimeOffset? studyDateTo,
        CancellationToken cancellationToken);
}
