namespace Dialysis.SmartConnect.Dicom;

/// <summary>
/// Notified after each DICOM instance is ingested. When the instance carries an Accession Number,
/// an implementation correlates the study back to its originating order (e.g. an EHR imaging order)
/// by publishing an integration event. The default <see cref="NoopImagingStudyLinkNotifier"/> does
/// nothing, so DICOM ingestion has no hard dependency on any consuming module — a host opts in by
/// registering a real notifier (the <c>Dicom.Integration</c> bridge).
/// </summary>
public interface IImagingStudyLinkNotifier
{
    Task NotifyInstanceIngestedAsync(DicomInstanceMetadata metadata, CancellationToken cancellationToken);
}

/// <summary>No-op default: DICOM store works standalone with no module coupling.</summary>
public sealed class NoopImagingStudyLinkNotifier : IImagingStudyLinkNotifier
{
    public Task NotifyInstanceIngestedAsync(DicomInstanceMetadata metadata, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
