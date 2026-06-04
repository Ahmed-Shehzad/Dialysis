using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Integration.Domain;
using Dialysis.EHR.Integration.Ports;
using Dialysis.EHR.Integration.Translators;

namespace Dialysis.EHR.Integration.Consumers;

/// <summary>
/// Reacts to <see cref="PrescriptionOrderedIntegrationEvent"/> by queueing an outbound NCPDP SCRIPT
/// transmission to the pharmacy and invoking the wire gateway. The boundary between the upstream
/// ClinicalNotes-Prescription Published Language and the local PharmacyTransmission model is named
/// explicitly via <see cref="PrescriptionOrderedTranslator"/> (Evans Anticorruption Layer, pp. 258–260).
/// </summary>
public sealed class PrescriptionOrderedConsumer : IConsumer<PrescriptionOrderedIntegrationEvent>
{
    private readonly IPharmacyTransmissionRepository _transmissions;
    private readonly IPharmacyGateway _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    /// <summary>
    /// Reacts to <see cref="PrescriptionOrderedIntegrationEvent"/> by queueing an outbound NCPDP SCRIPT
    /// transmission to the pharmacy and invoking the wire gateway. The boundary between the upstream
    /// ClinicalNotes-Prescription Published Language and the local PharmacyTransmission model is named
    /// explicitly via <see cref="PrescriptionOrderedTranslator"/> (Evans Anticorruption Layer, pp. 258–260).
    /// </summary>
    public PrescriptionOrderedConsumer(IPharmacyTransmissionRepository transmissions,
        IPharmacyGateway gateway,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _transmissions = transmissions;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task HandleAsync(ConsumeContext<PrescriptionOrderedIntegrationEvent> context)
    {
        var translated = PrescriptionOrderedTranslator.Translate(context.Message);
        var id = Guid.CreateVersion7();
        var transmission = PharmacyTransmission.Queue(
            id,
            translated.PrescriptionId,
            translated.PharmacyNcpdpId,
            translated.TransmissionFormat,
            translated.PayloadDigest);

        try
        {
            var controlNumber = await _gateway.TransmitAsync(transmission, context.CancellationToken).ConfigureAwait(false);
            transmission.RecordSent(controlNumber, _timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (Exception ex)
        {
            transmission.RecordFailure(ex.GetType().Name, _timeProvider.GetUtcNow().UtcDateTime);
        }

        _transmissions.Add(transmission);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}
