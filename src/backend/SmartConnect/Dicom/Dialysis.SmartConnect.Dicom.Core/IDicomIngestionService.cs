using FellowOakDicom;

namespace Dialysis.SmartConnect.Dicom;

/// <summary>
/// Ingests one DICOM SOP Instance: extracts the searchable metadata, stores the .dcm bytes in the
/// attachment blob store, and inserts a row into the <see cref="IDicomInstanceStore"/>. Returns the
/// metadata so callers (STOW-RS / DIMSE C-STORE) can build the success response.
/// </summary>
public interface IDicomIngestionService
{
    Task<DicomInstanceMetadata> IngestAsync(DicomFile dicomFile, CancellationToken cancellationToken);
}
