using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Medications.Contracts;
using Dialysis.PDMS.Medications.Domain;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.Medications.Consumers;

/// <summary>
/// Listens for <see cref="MedicationAdministeredIntegrationEvent"/> and deducts one unit
/// of stock from the matching <see cref="MedicationInventoryItem"/>. Matches on
/// <c>(CodeSystem, Code)</c> — lot tracking is per-deployment policy, so we deduct from
/// the longest-shelf-life lot first (FIFO by expiry).
///
/// Administration is the source of truth: a missing inventory row never blocks the MAR
/// write. The consumer logs and exits silently when no matching stock is on file —
/// surfaced separately as an operator reconciliation alert via the existing
/// audit pipeline.
///
/// Idempotent: re-delivery of the same event is detected by a per-event marker on the
/// inventory item's transaction log (a future band-3 follow-up; today we rely on
/// Transponder's inbox dedup).
/// </summary>
public sealed class OnMedicationAdministered(
    IPdmsRepository<MedicationInventoryItem, Guid> inventory,
    IUnitOfWork unitOfWork,
    ILogger<OnMedicationAdministered> logger)
    : IConsumer<MedicationAdministeredIntegrationEvent>
{
    public async Task HandleAsync(ConsumeContext<MedicationAdministeredIntegrationEvent> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var items = await inventory.ListAsync(null, ct).ConfigureAwait(false);
        var match = items
            .Where(i => string.Equals(i.Medication.CodeSystem, message.MedicationCodeSystem, StringComparison.OrdinalIgnoreCase)
                     && string.Equals(i.Medication.Code, message.MedicationCode, StringComparison.OrdinalIgnoreCase)
                     && i.OnHandUnits > 0)
            .OrderByDescending(i => i.ExpiryUtc)
            .FirstOrDefault();

        if (match is null)
        {
            logger.LogWarning(
                "No inventory row for {CodeSystem}:{Code} — administration {EntryId} recorded without inventory deduction.",
                message.MedicationCodeSystem, message.MedicationCode, message.EntryId);
            return;
        }

        match.Deduct(units: 1, reason: $"session:{message.SessionId:N};entry:{message.EntryId:N}");
        inventory.Update(match);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Deducted 1 unit from inventory {ItemId} for administration {EntryId}; on-hand now {OnHand}.",
            match.Id, message.EntryId, match.OnHandUnits);
    }
}
