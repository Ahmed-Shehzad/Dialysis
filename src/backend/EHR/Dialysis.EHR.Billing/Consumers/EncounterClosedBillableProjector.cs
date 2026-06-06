using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.Billing.Consumers;

/// <summary>
/// Records every closed encounter into the Billing-owned <c>BillableEncounter</c> read model so the
/// revenue-cycle worklist can find encounters that never produced a charge. Always on (independent of
/// the auto-capture flag) — the lost-charge worklist matters most when charges are entered manually.
/// </summary>
public sealed class EncounterClosedBillableProjector : IConsumer<EncounterClosedIntegrationEvent>
{
    private readonly IBillableEncounterRepository _billable;
    private readonly IUnitOfWork _unitOfWork;
    public EncounterClosedBillableProjector(IBillableEncounterRepository billable, IUnitOfWork unitOfWork)
    {
        _billable = billable;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(ConsumeContext<EncounterClosedIntegrationEvent> context)
    {
        var m = context.Message;
        await _billable.UpsertAsync(m.EncounterId, m.PatientId, m.ProviderId, m.ClosedAtUtc, context.CancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}
