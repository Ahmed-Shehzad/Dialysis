using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderPrescription;

public sealed class OrderPrescriptionCommandHandler : ICommandHandler<OrderPrescriptionCommand, Guid>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IUnitOfWork _unitOfWork;
    public OrderPrescriptionCommandHandler(IPrescriptionRepository prescriptions,
        IUnitOfWork unitOfWork)
    {
        _prescriptions = prescriptions;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(OrderPrescriptionCommand request, CancellationToken cancellationToken)
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
        _prescriptions.Add(prescription);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
