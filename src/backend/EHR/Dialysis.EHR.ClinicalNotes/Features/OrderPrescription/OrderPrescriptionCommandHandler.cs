using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderPrescription;

public sealed class OrderPrescriptionCommandHandler : ICommandHandler<OrderPrescriptionCommand, OrderPlacementResult>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IClinicalSafetyChecker _safety;
    private readonly IUnitOfWork _unitOfWork;
    public OrderPrescriptionCommandHandler(IPrescriptionRepository prescriptions,
        IClinicalSafetyChecker safety,
        IUnitOfWork unitOfWork)
    {
        _prescriptions = prescriptions;
        _safety = safety;
        _unitOfWork = unitOfWork;
    }
    public async Task<OrderPlacementResult> HandleAsync(OrderPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var safety = await _safety.CheckPrescriptionAsync(
            request.PatientId, request.MedicationRxnormCode, request.MedicationDisplay, cancellationToken)
            .ConfigureAwait(false);

        // Blocking advisories stop the order unless the clinician explicitly acknowledged with a reason.
        if (safety.HasBlocking && !(request.AcknowledgeAdvisories && !string.IsNullOrWhiteSpace(request.OverrideReason)))
            throw new ClinicalSafetyBlockedException(safety.Advisories);

        var overrode = safety.HasBlocking && request.AcknowledgeAdvisories;

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
            request.TransmissionFormat,
            overrideReason: overrode ? request.OverrideReason : null,
            overriddenBy: overrode ? request.OverriddenBy : null);
        _prescriptions.Add(prescription);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new OrderPlacementResult(id, safety.Advisories);
    }
}
