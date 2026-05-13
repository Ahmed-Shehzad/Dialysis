using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderPrescription;

public sealed class OrderPrescriptionCommandHandler(
    IPrescriptionRepository prescriptions,
    IUnitOfWork unitOfWork)
    : ICommandHandler<OrderPrescriptionCommand, Guid>
{
    public async Task<Guid> Handle(OrderPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var prescription = Prescription.Order(
            id,
            request.PatientId,
            request.EncounterId,
            request.PrescribingProviderId,
            request.MedicationRxnormCode,
            request.MedicationDisplay,
            request.DoseText,
            request.FrequencyText,
            request.QuantityDispensed,
            request.RefillsAuthorized,
            request.PharmacyNcpdpId,
            request.TransmissionFormat);
        prescriptions.Add(prescription);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
