namespace Dialysis.SmartConnect.Dicom;

/// <summary>
/// Identifies a single DICOM SOP Instance within the three-level study → series → instance hierarchy.
/// Each level has a globally-unique UID (per DICOM spec, never to be reused). <see cref="BlobId"/>
/// is the SmartConnect attachment id where the .dcm bytes live.
/// </summary>
public sealed record DicomInstanceMetadata
{
    /// <summary>
    /// Identifies a single DICOM SOP Instance within the three-level study → series → instance hierarchy.
    /// Each level has a globally-unique UID (per DICOM spec, never to be reused). <see cref="BlobId"/>
    /// is the SmartConnect attachment id where the .dcm bytes live.
    /// </summary>
    public DicomInstanceMetadata(string StudyInstanceUid,
        string SeriesInstanceUid,
        string SopInstanceUid,
        string SopClassUid,
        string? PatientId,
        string? PatientName,
        string? Modality,
        DateTimeOffset ReceivedUtc,
        long SizeBytes,
        Guid BlobId)
    {
        this.StudyInstanceUid = StudyInstanceUid;
        this.SeriesInstanceUid = SeriesInstanceUid;
        this.SopInstanceUid = SopInstanceUid;
        this.SopClassUid = SopClassUid;
        this.PatientId = PatientId;
        this.PatientName = PatientName;
        this.Modality = Modality;
        this.ReceivedUtc = ReceivedUtc;
        this.SizeBytes = SizeBytes;
        this.BlobId = BlobId;
    }
    public string StudyInstanceUid { get; init; }
    public string SeriesInstanceUid { get; init; }
    public string SopInstanceUid { get; init; }
    public string SopClassUid { get; init; }
    public string? PatientId { get; init; }
    public string? PatientName { get; init; }
    public string? Modality { get; init; }
    public DateTimeOffset ReceivedUtc { get; init; }
    public long SizeBytes { get; init; }
    public Guid BlobId { get; init; }
    public void Deconstruct(out string StudyInstanceUid, out string SeriesInstanceUid, out string SopInstanceUid, out string SopClassUid, out string? PatientId, out string? PatientName, out string? Modality, out DateTimeOffset ReceivedUtc, out long SizeBytes, out Guid BlobId)
    {
        StudyInstanceUid = this.StudyInstanceUid;
        SeriesInstanceUid = this.SeriesInstanceUid;
        SopInstanceUid = this.SopInstanceUid;
        SopClassUid = this.SopClassUid;
        PatientId = this.PatientId;
        PatientName = this.PatientName;
        Modality = this.Modality;
        ReceivedUtc = this.ReceivedUtc;
        SizeBytes = this.SizeBytes;
        BlobId = this.BlobId;
    }
}

/// <summary>
/// Aggregate of one DICOM Study and the series + instances it contains. Built by querying the
/// instance metadata table by <see cref="StudyInstanceUid"/>.
/// </summary>
public sealed record DicomStudy
{
    /// <summary>
    /// Aggregate of one DICOM Study and the series + instances it contains. Built by querying the
    /// instance metadata table by <see cref="StudyInstanceUid"/>.
    /// </summary>
    public DicomStudy(string StudyInstanceUid,
        string? PatientId,
        string? PatientName,
        DateTimeOffset ReceivedUtc,
        IReadOnlyList<DicomSeries> Series)
    {
        this.StudyInstanceUid = StudyInstanceUid;
        this.PatientId = PatientId;
        this.PatientName = PatientName;
        this.ReceivedUtc = ReceivedUtc;
        this.Series = Series;
    }
    public string StudyInstanceUid { get; init; }
    public string? PatientId { get; init; }
    public string? PatientName { get; init; }
    public DateTimeOffset ReceivedUtc { get; init; }
    public IReadOnlyList<DicomSeries> Series { get; init; }
    public void Deconstruct(out string StudyInstanceUid, out string? PatientId, out string? PatientName, out DateTimeOffset ReceivedUtc, out IReadOnlyList<DicomSeries> Series)
    {
        StudyInstanceUid = this.StudyInstanceUid;
        PatientId = this.PatientId;
        PatientName = this.PatientName;
        ReceivedUtc = this.ReceivedUtc;
        Series = this.Series;
    }
}

/// <summary>One series under a study. Instances are leaf .dcm blobs.</summary>
public sealed record DicomSeries
{
    /// <summary>One series under a study. Instances are leaf .dcm blobs.</summary>
    public DicomSeries(string SeriesInstanceUid,
        string? Modality,
        IReadOnlyList<DicomInstanceMetadata> Instances)
    {
        this.SeriesInstanceUid = SeriesInstanceUid;
        this.Modality = Modality;
        this.Instances = Instances;
    }
    public string SeriesInstanceUid { get; init; }
    public string? Modality { get; init; }
    public IReadOnlyList<DicomInstanceMetadata> Instances { get; init; }
    public void Deconstruct(out string SeriesInstanceUid, out string? Modality, out IReadOnlyList<DicomInstanceMetadata> Instances)
    {
        SeriesInstanceUid = this.SeriesInstanceUid;
        Modality = this.Modality;
        Instances = this.Instances;
    }
}
