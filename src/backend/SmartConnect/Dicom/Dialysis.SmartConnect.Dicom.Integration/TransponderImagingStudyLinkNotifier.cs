using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.SmartConnect.Dicom.Ai;

namespace Dialysis.SmartConnect.Dicom.Integration;

/// <summary>
/// The DICOM → EHR producer bridge. When a STOW'd (or C-STORE'd) instance carries an Accession
/// Number it: (1) publishes <see cref="ImagingStudyLinkedIntegrationEvent"/> so EHR correlates the
/// study back to its order and completes it; and (2) when AI imaging is enabled, runs the governed
/// <see cref="ImagingAiAnalyzer"/> and publishes <see cref="ImagingAiFindingProducedIntegrationEvent"/>
/// (advisory, human-in-the-loop). The single point where the DICOM store couples to a module
/// contract — kept out of the standalone Core via the <see cref="IImagingStudyLinkNotifier"/> seam,
/// and opt-in (a host registers this notifier).
///
/// One event is emitted per ingested instance; the EHR consumers are idempotent (a study already
/// linked / a finding already reviewed is skipped), so per-instance emission is safe.
/// </summary>
public sealed class TransponderImagingStudyLinkNotifier : IImagingStudyLinkNotifier
{
    private readonly ITransponderBus _bus;
    private readonly ImagingAiAnalyzer _aiAnalyzer;

    /// <summary>Creates the notifier with the bus and the (flag-gated) AI analyzer.</summary>
    public TransponderImagingStudyLinkNotifier(ITransponderBus bus, ImagingAiAnalyzer aiAnalyzer)
    {
        _bus = bus;
        _aiAnalyzer = aiAnalyzer;
    }

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

        await _bus.PublishAsync(
            new ImagingStudyLinkedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: DateTime.UtcNow,
                SchemaVersion: 1,
                ImagingOrderId: Guid.Empty,
                PatientId: patientId,
                AccessionNumber: metadata.AccessionNumber,
                StudyInstanceUid: metadata.StudyInstanceUid,
                SeriesCount: 1,
                InstanceCount: 1),
            cancellationToken).ConfigureAwait(false);

        await PublishAiFindingIfAnyAsync(metadata, patientId, cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishAiFindingIfAnyAsync(DicomInstanceMetadata metadata, Guid patientId, CancellationToken cancellationToken)
    {
        var assessment = await _aiAnalyzer.AnalyzeAsync(
            new ImagingInferenceRequest(metadata.StudyInstanceUid, metadata.Modality, BodySiteHint: null, metadata.AccessionNumber),
            cancellationToken).ConfigureAwait(false);
        if (assessment is null)
        {
            return; // AI disabled, or no qualifying finding
        }

        var f = assessment.Finding;
        await _bus.PublishAsync(
            new ImagingAiFindingProducedIntegrationEvent(
                EventId: Guid.CreateVersion7(),
                OccurredOn: DateTime.UtcNow,
                SchemaVersion: 1,
                AccessionNumber: metadata.AccessionNumber!,
                StudyInstanceUid: metadata.StudyInstanceUid,
                PatientId: patientId,
                ModelId: assessment.ModelId,
                FindingCode: f.Code,
                FindingSystem: f.System,
                FindingDisplay: f.Display,
                Confidence: f.Confidence,
                Interpretation: f.Interpretation.ToString(),
                Summary: f.Summary,
                RequiresHumanReview: assessment.RequiresHumanReview),
            cancellationToken).ConfigureAwait(false);
    }
}
