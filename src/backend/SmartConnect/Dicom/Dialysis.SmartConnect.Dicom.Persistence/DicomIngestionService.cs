using Dialysis.SmartConnect.Attachments;
using FellowOakDicom;

namespace Dialysis.SmartConnect.Dicom.Persistence;

/// <summary>
/// Default ingestion: writes the .dcm bytes to <see cref="IAttachmentBlobStore"/>, records the
/// header metadata in <see cref="IDicomInstanceStore"/>, returns the resulting metadata. Order
/// matters — blob first so a metadata-save failure leaves the bytes for the orphan reaper to
/// remove, rather than a metadata row pointing at nothing.
/// </summary>
public sealed class DicomIngestionService(
    IAttachmentBlobStore blobs,
    IDicomInstanceStore instances,
    TimeProvider timeProvider) : IDicomIngestionService
{
    public async Task<DicomInstanceMetadata> IngestAsync(DicomFile dicomFile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dicomFile);
        var ds = dicomFile.Dataset ?? throw new ArgumentException("DicomFile has no dataset", nameof(dicomFile));

        var studyUid = ds.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);
        var seriesUid = ds.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);
        var sopUid = ds.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);
        var sopClassUid = ds.GetSingleValueOrDefault(DicomTag.SOPClassUID, string.Empty);
        var patientId = ds.GetSingleValueOrDefault<string?>(DicomTag.PatientID, null);
        var patientName = ds.GetSingleValueOrDefault<string?>(DicomTag.PatientName, null);
        var modality = ds.GetSingleValueOrDefault<string?>(DicomTag.Modality, null);

        if (string.IsNullOrEmpty(studyUid) || string.IsNullOrEmpty(seriesUid) || string.IsNullOrEmpty(sopUid))
        {
            throw new InvalidOperationException(
                "DICOM file is missing one or more of StudyInstanceUID / SeriesInstanceUID / SOPInstanceUID.");
        }

        using var stream = new MemoryStream();
        await dicomFile.SaveAsync(stream).ConfigureAwait(false);
        var bytes = stream.ToArray();

        var blobId = Guid.CreateVersion7();
        await blobs.WriteAsync(blobId, bytes, cancellationToken).ConfigureAwait(false);

        var metadata = new DicomInstanceMetadata(
            StudyInstanceUid: studyUid,
            SeriesInstanceUid: seriesUid,
            SopInstanceUid: sopUid,
            SopClassUid: sopClassUid,
            PatientId: patientId,
            PatientName: patientName,
            Modality: modality,
            ReceivedUtc: timeProvider.GetUtcNow(),
            SizeBytes: bytes.LongLength,
            BlobId: blobId);

        await instances.AddAsync(metadata, cancellationToken).ConfigureAwait(false);
        return metadata;
    }
}
