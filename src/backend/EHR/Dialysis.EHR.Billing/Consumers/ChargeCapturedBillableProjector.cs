using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Billing.Ports;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.Billing.Consumers;

/// <summary>
/// Flips <c>HasCharge</c> on the <c>BillableEncounter</c> read model when a charge is captured for that
/// encounter, so it drops off the lost-charge worklist. Charges with no matching billable-encounter row
/// (e.g. PDMS session charges, whose session id isn't a closed clinical encounter) are a harmless no-op.
/// </summary>
public sealed class ChargeCapturedBillableProjector : IConsumer<ChargeCapturedIntegrationEvent>
{
    private readonly IBillableEncounterRepository _billable;
    public ChargeCapturedBillableProjector(IBillableEncounterRepository billable) => _billable = billable;

    public Task HandleAsync(ConsumeContext<ChargeCapturedIntegrationEvent> context) =>
        _billable.MarkHasChargeAsync(context.Message.EncounterId, context.CancellationToken);
}
