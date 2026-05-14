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
public sealed class PrescriptionOrderedConsumer(
    IPharmacyTransmissionRepository transmissions,
    IPharmacyGateway gateway,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IConsumer<PrescriptionOrderedIntegrationEvent>
{
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
            var controlNumber = await gateway.TransmitAsync(transmission, context.CancellationToken).ConfigureAwait(false);
            transmission.RecordSent(controlNumber, timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (Exception ex)
        {
            transmission.RecordFailure(ex.GetType().Name, timeProvider.GetUtcNow().UtcDateTime);
        }

        transmissions.Add(transmission);
        await unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}
