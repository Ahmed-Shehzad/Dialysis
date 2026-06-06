using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Features.OrderImagingStudy;
using Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;
using Dialysis.EHR.ClinicalNotes.Features.OrderPrescription;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderSets;

public sealed class CreateOrderSetCommandHandler : ICommandHandler<CreateOrderSetCommand, Guid>
{
    private readonly IOrderSetRepository _orderSets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public CreateOrderSetCommandHandler(IOrderSetRepository orderSets, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _orderSets = orderSets;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Guid> HandleAsync(CreateOrderSetCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var set = OrderSet.Create(id, request.Name, request.Description, _timeProvider.GetUtcNow().UtcDateTime);
        foreach (var l in request.LabLines ?? [])
            set.AddLabLine(Guid.CreateVersion7(), l.LabFacilityCode, l.LoincPanelCodes);
        foreach (var m in request.MedicationLines ?? [])
            set.AddMedicationLine(Guid.CreateVersion7(), m.MedicationRxnormCode, m.MedicationDisplay, m.DoseText, m.FrequencyText, m.QuantityDispensed, m.RefillsAuthorized, m.PharmacyNcpdpId);
        foreach (var i in request.ImagingLines ?? [])
            set.AddImagingLine(Guid.CreateVersion7(), i.ModalityCode, i.BodySiteCode, i.ReasonText);
        _orderSets.Add(set);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}

public sealed class DeactivateOrderSetCommandHandler : ICommandHandler<DeactivateOrderSetCommand, Unit>
{
    private readonly IOrderSetRepository _orderSets;
    private readonly IUnitOfWork _unitOfWork;
    public DeactivateOrderSetCommandHandler(IOrderSetRepository orderSets, IUnitOfWork unitOfWork)
    {
        _orderSets = orderSets;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(DeactivateOrderSetCommand request, CancellationToken cancellationToken)
    {
        var set = await _orderSets.GetAsync(request.OrderSetId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Order set not found.");
        set.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

/// <summary>
/// Applies an order set by dispatching each line through the existing order commands via the CQRS
/// gateway — so the Phase 6a safety checks and per-order permission gating run for every line. Not
/// atomic (each sub-handler saves on its own); a blocking advisory fails the whole set unless
/// acknowledged with a reason.
/// </summary>
public sealed class ApplyOrderSetCommandHandler : ICommandHandler<ApplyOrderSetCommand, ApplyOrderSetResult>
{
    private readonly IOrderSetRepository _orderSets;
    private readonly ICqrsGateway _gateway;
    public ApplyOrderSetCommandHandler(IOrderSetRepository orderSets, ICqrsGateway gateway)
    {
        _orderSets = orderSets;
        _gateway = gateway;
    }
    public async Task<ApplyOrderSetResult> HandleAsync(ApplyOrderSetCommand request, CancellationToken cancellationToken)
    {
        var set = await _orderSets.GetAsync(request.OrderSetId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Order set not found.");

        var orders = new List<AppliedOrder>();
        var advisories = new List<SafetyAdvisory>();

        foreach (var line in set.Lines)
        {
            switch (line.Kind)
            {
                case OrderSetLineKind.Lab:
                {
                    var result = await _gateway.SendCommandAsync<OrderLabTestCommand, OrderPlacementResult>(
                        new OrderLabTestCommand(request.PatientId, request.EncounterId, request.OrderingProviderId,
                            line.LabFacilityCode!, [.. line.LoincPanelCodes],
                            AcknowledgeAdvisories: request.AcknowledgeAdvisories,
                            OverrideReason: request.OverrideReason, OverriddenBy: request.OverriddenBy),
                        cancellationToken).ConfigureAwait(false);
                    orders.Add(new AppliedOrder("Lab", result.Id));
                    advisories.AddRange(result.Advisories);
                    break;
                }
                case OrderSetLineKind.Medication:
                {
                    var result = await _gateway.SendCommandAsync<OrderPrescriptionCommand, OrderPlacementResult>(
                        new OrderPrescriptionCommand(request.PatientId, request.EncounterId, request.OrderingProviderId,
                            line.MedicationRxnormCode!, line.MedicationDisplay!, line.DoseText!, line.FrequencyText!,
                            line.QuantityDispensed!.Value, line.RefillsAuthorized!.Value, line.PharmacyNcpdpId!,
                            AcknowledgeAdvisories: request.AcknowledgeAdvisories,
                            OverrideReason: request.OverrideReason, OverriddenBy: request.OverriddenBy),
                        cancellationToken).ConfigureAwait(false);
                    orders.Add(new AppliedOrder("Medication", result.Id));
                    advisories.AddRange(result.Advisories);
                    break;
                }
                case OrderSetLineKind.Imaging:
                {
                    var id = await _gateway.SendCommandAsync<OrderImagingStudyCommand, Guid>(
                        new OrderImagingStudyCommand(request.PatientId, request.EncounterId, request.OrderingProviderId,
                            line.ModalityCode!, line.BodySiteCode!, line.ReasonText),
                        cancellationToken).ConfigureAwait(false);
                    orders.Add(new AppliedOrder("Imaging", id));
                    break;
                }
            }
        }

        return new ApplyOrderSetResult(orders, advisories);
    }
}
