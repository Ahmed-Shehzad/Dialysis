using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.SmartConnect.Dicom.Integration;

/// <summary>
/// The DICOM → EHR producer bridge: when a STOW'd (or C-STORE'd) instance carries an Accession
/// Number, publishes <see cref="ImagingStudyLinkedIntegrationEvent"/> so the EHR imaging-order
/// consumer can correlate the study back to its order (by accession) and complete it. The single
/// point where the DICOM store couples to a module contract — kept out of the standalone Core via
/// the <see cref="IImagingStudyLinkNotifier"/> seam, and opt-in (a host registers this notifier).
///
/// One event is emitted per ingested instance; the consumer is idempotent (it ignores an event for
/// a study already linked to the order), so per-instance emission is safe.
/// </summary>
public sealed class TransponderImagingStudyLinkNotifier : IImagingStudyLinkNotifier
{
    private readonly ITransponderBus _bus;

    /// <summary>Creates the notifier with the bus it publishes the linked event on.</summary>
    public TransponderImagingStudyLinkNotifier(ITransponderBus bus) => _bus = bus;

    /// <inheritdoc />
    public async Task NotifyInstanceIngestedAsync(DicomInstanceMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (string.IsNullOrWhiteSpace(metadata.AccessionNumber) || string.IsNullOrWhiteSpace(metadata.StudyInstanceUid))
        {
            return;
        }

        // The EHR consumer correlates by accession number and reads PatientId from the order itself,
        // so ImagingOrderId is left empty here; PatientId is forwarded when the DICOM PatientID is a
        // platform patient guid, otherwise empty.
        var patientId = Guid.TryParse(metadata.PatientId, out var parsed) ? parsed : Guid.Empty;

        var @event = new ImagingStudyLinkedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            ImagingOrderId: Guid.Empty,
            PatientId: patientId,
            AccessionNumber: metadata.AccessionNumber,
            StudyInstanceUid: metadata.StudyInstanceUid,
            SeriesCount: 1,
            InstanceCount: 1);

        await _bus.PublishAsync(@event, cancellationToken).ConfigureAwait(false);
    }
}
