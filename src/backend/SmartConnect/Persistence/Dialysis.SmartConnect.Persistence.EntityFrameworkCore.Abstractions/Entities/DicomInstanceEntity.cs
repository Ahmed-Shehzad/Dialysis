namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

/// <summary>
/// Searchable header row for one DICOM SOP Instance. The .dcm bytes live in the attachment blob
/// store keyed by <see cref="BlobId"/>; this entity is what QIDO-RS queries hit when clients
/// search for studies / series / instances by patient + date.
/// </summary>
public sealed class DicomInstanceEntity
{
    public Guid Id { get; set; }
    public string StudyInstanceUid { get; set; } = string.Empty;
    public string SeriesInstanceUid { get; set; } = string.Empty;
    public string SopInstanceUid { get; set; } = string.Empty;
    public string SopClassUid { get; set; } = string.Empty;
    public string? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? Modality { get; set; }

    /// <summary>DICOM Accession Number (0008,0050) — links the study to its originating order.</summary>
    public string? AccessionNumber { get; set; }
    public DateTimeOffset ReceivedUtc { get; set; }
    public long SizeBytes { get; set; }
    /// <summary>Foreign key into <see cref="AttachmentEntity"/>.</summary>
    public Guid BlobId { get; set; }
}
