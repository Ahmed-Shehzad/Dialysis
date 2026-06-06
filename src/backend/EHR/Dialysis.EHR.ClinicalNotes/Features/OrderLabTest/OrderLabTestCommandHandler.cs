using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;

public sealed class OrderLabTestCommandHandler : ICommandHandler<OrderLabTestCommand, OrderPlacementResult>
{
    private readonly ILabOrderRepository _labOrders;
    private readonly IClinicalSafetyChecker _safety;
    private readonly IUnitOfWork _unitOfWork;
    public OrderLabTestCommandHandler(ILabOrderRepository labOrders,
        IClinicalSafetyChecker safety,
        IUnitOfWork unitOfWork)
    {
        _labOrders = labOrders;
        _safety = safety;
        _unitOfWork = unitOfWork;
    }
    public async Task<OrderPlacementResult> HandleAsync(OrderLabTestCommand request, CancellationToken cancellationToken)
    {
        var safety = await _safety.CheckLabOrderAsync(request.PatientId, request.LoincPanelCodes, cancellationToken)
            .ConfigureAwait(false);

        // Duplicate-lab advisories are warnings, never blocking — but honour the same override contract
        // so a future blocking lab rule (and the audited trail) flows through unchanged.
        if (safety.HasBlocking && !(request.AcknowledgeAdvisories && !string.IsNullOrWhiteSpace(request.OverrideReason)))
            throw new ClinicalSafetyBlockedException(safety.Advisories);

        var overrode = safety.HasBlocking && request.AcknowledgeAdvisories;

        var id = Guid.CreateVersion7();
        var order = LabOrder.Order(
            id,
            request.PatientId,
            request.EncounterId,
            request.OrderingProviderId,
            request.LabFacilityCode,
            request.LoincPanelCodes,
            request.TransmissionFormat,
            overrideReason: overrode ? request.OverrideReason : null,
            overriddenBy: overrode ? request.OverriddenBy : null);
        _labOrders.Add(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new OrderPlacementResult(id, safety.Advisories);
    }
}
